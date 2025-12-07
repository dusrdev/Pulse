using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

using Pulse.Models;

namespace Pulse.Core;


/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
internal sealed class PulseMonitor {
    /// <summary>
    /// Holds the results of all the requests
    /// </summary>
    private readonly ConcurrentStack<Response> _results;

    /// <summary>
    /// Timestamp of the beginning of monitoring
    /// </summary>
    private readonly long _start;

    /// <summary>
    /// Current number of responses received
    /// </summary>
    private PaddedULong _responses;

    // response status code counter
    // 0: exception
    // 1: 1xx
    // 2: 2xx
    // 3: 3xx
    // 4: 4xx
    // 5: 5xx
    private readonly PaddedULong[] _stats = new PaddedULong[6];
    private readonly ulong _requestCount;
    private readonly bool _saveContent;
    private readonly CancellationToken _cancellationToken;
    private readonly HttpClient _httpClient;
    private readonly Request _requestRecipe;
    private readonly Task _printer;
    private readonly bool _reportProgress;

    private readonly Channel<Stats> _channel;

    /// <summary>
    /// Creates a new pulse monitor
    /// </summary>
    public PulseMonitor(HttpClient client, Request requestRecipe, Parameters parameters) {
        _results = new ConcurrentStack<Response>();
        _requestCount = (ulong)parameters.Requests;
        _saveContent = parameters.Export;
        _cancellationToken = parameters.CancellationToken;
        _httpClient = client;
        _requestRecipe = requestRecipe;
        _reportProgress = !parameters.Quiet;
        _start = Stopwatch.GetTimestamp();

        if (_reportProgress) {
            _channel = Channel.CreateBounded<Stats>(new BoundedChannelOptions(1) {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropWrite
            });

            _ = _channel.Writer.TryWrite(new Stats {
                Percentage = 0,
                CurrentCount = _responses,
                SuccessRate = 0,
                Eta = TimeSpan.MaxValue,
                RequestCount = _requestCount,
                StatusCodes = _stats
            });

            Console.CursorVisible = false;
            ConsoleState.ReportLinesFromCurrent(3);

            _printer = Task.Run(async () => {
                await foreach (var stats in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false)) {
                    PrintMetrics(stats);
                }
            });
        } else {
            _channel = null!;

            _printer = Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(int requestId) {
        var result = await SendRequest(requestId, _requestRecipe, _httpClient, _saveContent, _cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _responses.Value);
        // Increment stats

        int index = (int)result.StatusCode / 100;
        ref var bucket = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_stats), index);
        Interlocked.Increment(ref bucket.Value);
        // Print metrics

        if (_reportProgress) {
            await PushMetricsAsync().ConfigureAwait(false);
        }
        _results.Push(result);
    }

    /// <summary>
    /// Handles printing the current metrics, has to be synchronized to prevent cross writing to the console, which produces corrupted output.
    /// </summary>
    private async ValueTask PushMetricsAsync() {
        var percentage = Helper.Percentage<double>(_responses.Value, _requestCount);
        var eta = Helper.GetEta(percentage, Stopwatch.GetElapsedTime(_start));
        double sr = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2);

        var stats = new Stats {
            Percentage = percentage,
            CurrentCount = _responses,
            RequestCount = _requestCount,
            StatusCodes = _stats,
            Eta = eta,
            SuccessRate = sr
        };

        await _channel.Writer.WriteAsync(stats, _cancellationToken).ConfigureAwait(false);
    }

    private static void PrintMetrics(Stats stats) {
        Console.Overwrite(stats, static s => {
            Console.WriteInterpolated(OutputPipe.Error, $"Completed: {Yellow}{s.CurrentCount.Value}{ConsoleColor.Default}/{Yellow}{s.RequestCount}{ConsoleColor.Default} ");
            ProgressBar.WriteProgressBar(OutputPipe.Error, s.Percentage * 100, Green, maxLineWidth: 34);
            Console.NewLine(OutputPipe.Error);
            Console.WriteLineInterpolated(OutputPipe.Error, $"Success Rate: {Helper.GetPercentageBasedColor(s.SuccessRate)}{s.SuccessRate}{ConsoleColor.Default}%, Estimated time remaining: {Yellow}{s.Eta:duration}");
            Console.WriteLineInterpolated(OutputPipe.Error, $"1xx: {White}{s.StatusCodes[1].Value}{ConsoleColor.Default}, 2xx: {Green}{s.StatusCodes[2].Value}{ConsoleColor.Default}, 3xx: {Yellow}{s.StatusCodes[3].Value}{ConsoleColor.Default}, 4xx: {Red}{s.StatusCodes[4].Value}{ConsoleColor.Default}, 5xx: {Red}{s.StatusCodes[5].Value}{ConsoleColor.Default}, others: {Magenta}{s.StatusCodes[0].Value}");
        }, 3);
    }

    private PaddedULong _currentConcurrentConnections;

    /// <summary>
    /// Sends a request.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="requestRecipe">The recipe used to build the request message.</param>
    /// <param name="httpClient">Configured <see cref="HttpClient"/> instance.</param>
    /// <param name="saveContent">Whether response content should be persisted.</param>
    /// <param name="cancellationToken">Cancellation token for the i/o operation.</param>
    /// <returns>A <see cref="Response"/> describing the outcome.</returns>
    public async Task<Response> SendRequest(int id, Request requestRecipe, HttpClient httpClient, bool saveContent, CancellationToken cancellationToken) {
        HttpStatusCode statusCode = 0;
        string content = string.Empty;
        long contentLength = 0;
        int currentConcurrencyLevel = 0;
        StrippedException exception = StrippedException.Default;
        var headers = Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
        using var message = requestRecipe.CreateMessage();
        long start = Stopwatch.GetTimestamp();
        HttpResponseMessage? response = null;
        try {
            currentConcurrencyLevel = (int)Interlocked.Increment(ref _currentConcurrentConnections.Value);
            response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        } catch (TimeoutException ex) {
            exception = StrippedException.FromException(ex);
        } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && ex.InnerException is TimeoutException timeoutEx) {
            exception = StrippedException.FromException(timeoutEx);
        } finally {
            Interlocked.Decrement(ref _currentConcurrentConnections.Value);
        }
        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
        if (!exception.IsDefault) {
            return new Response {
                Id = id,
                StatusCode = statusCode,
                Headers = headers,
                Content = content,
                ContentLength = contentLength,
                Latency = elapsed,
                Exception = exception,
                CurrentConcurrentConnections = currentConcurrencyLevel
            };
        }

        try {
            var r = response!;
            statusCode = r.StatusCode;
            headers = r.Headers;
            var length = r.Content.Headers.ContentLength;
            if (length.HasValue) {
                contentLength = length.Value;
            }
            if (saveContent) {
                content = await r.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (contentLength == 0) {
                    var charSet = r.Content.Headers.ContentType?.CharSet;
                    var encoding = charSet is null
                        ? Encoding.UTF8
                        : Encoding.GetEncoding(charSet.Trim('"'));
                    contentLength = encoding.GetByteCount(content.AsSpan());
                }
            }
        } finally {
            response?.Dispose();
        }
        return new Response {
            Id = id,
            StatusCode = statusCode,
            Headers = headers,
            Content = content,
            ContentLength = contentLength,
            Latency = elapsed,
            Exception = exception,
            CurrentConcurrentConnections = currentConcurrencyLevel
        };
    }

    private readonly struct Stats {
        public required PaddedULong CurrentCount { get; init; }
        public required PaddedULong[] StatusCodes { get; init; }
        public required double Percentage { get; init; }
        public required TimeSpan Eta { get; init; }
        public required double SuccessRate { get; init; }
        public required ulong RequestCount { get; init; }
    }

    /// <inheritdoc />
    public async Task<PulseResult> ClearAndReturnAsync() {
        if (_reportProgress) {
            // Clear after metrics
            _channel.Writer.Complete();
            await _printer.ConfigureAwait(false);
            Console.ClearNextLines(3);
            Console.CursorVisible = true;
        }

        return new PulseResult {
            Results = _results,
            SuccessRate = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2),
            TotalDuration = Stopwatch.GetElapsedTime(_start)
        };
    }
}
