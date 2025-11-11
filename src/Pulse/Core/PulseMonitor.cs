using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

using Pulse.Configuration;

using static Pulse.Core.IPulseMonitor;

namespace Pulse.Core;


/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
internal sealed class PulseMonitor : IPulseMonitor {
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
    private readonly RequestExecutionContext _requestExecutionContext;
    private readonly ulong _requestCount;
    private readonly bool _saveContent;
    private readonly CancellationToken _cancellationToken;
    private readonly HttpClient _httpClient;
    private readonly Request _requestRecipe;
    private readonly Task _printer;

    private readonly Channel<Stats> _channel = Channel.CreateBounded<Stats>(new BoundedChannelOptions(1) {
        SingleWriter = false,
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropWrite
    });

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
        _requestExecutionContext = new RequestExecutionContext();
        _start = Stopwatch.GetTimestamp();

        _ = _channel.Writer.TryWrite(new Stats {
            Percentage = 0,
            CurrentCount = _responses,
            SuccessRate = 0,
            ETA = TimeSpan.MaxValue,
            RequestCount = _requestCount,
            StatusCodes = _stats
        });

        System.Console.CursorVisible = false;
        ConsoleState.ReportLinesFromCurrent(3);

        _printer = Task.Run(async () => {
            await foreach (var stats in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false)) {
                PrintMetrics(stats);
            }
        });
    }

    /// <inheritdoc />
    public async Task SendAsync(int requestId) {
        var result = await _requestExecutionContext.SendRequest(requestId, _requestRecipe, _httpClient, _saveContent, _cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _responses.Value);
        // Increment stats

        int index = (int)result.StatusCode / 100;
        Interlocked.Increment(ref _stats[index].Value);
        // Print metrics

        await PushMetricsAsync().ConfigureAwait(false);
        _results.Push(result);
    }

    /// <summary>
    /// Handles printing the current metrics, has to be synchronized to prevent cross writing to the console, which produces corrupted output.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask PushMetricsAsync() {
        var percentage = (double)_responses.Value / _requestCount;
        var eta = Helper.GetETA(percentage, Stopwatch.GetElapsedTime(_start));
        double sr = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2);

        var stats = new Stats {
            Percentage = percentage,
            CurrentCount = _responses,
            RequestCount = _requestCount,
            StatusCodes = _stats,
            ETA = eta,
            SuccessRate = sr
        };

        await _channel.Writer.WriteAsync(stats, _cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintMetrics(Stats stats) {
        Overwrite(stats, static s => {
            Write(OutputPipe.Error, $"Completed: {Yellow}{s.CurrentCount.Value}{Default}/{Yellow}{s.RequestCount}{Default} ");
            ProgressBar.WriteProgressBar(OutputPipe.Error, s.Percentage * 100, Green);
            WriteLine(OutputPipe.Error, $"Success Rate: {Helper.GetPercentageBasedColor(s.SuccessRate)}{s.SuccessRate}{Default}%, Estimated time remaining: {Yellow}{s.ETA:hr}");
            WriteLine(OutputPipe.Error, $"1xx: {White}{s.StatusCodes[1].Value}{Default}, 2xx: {Green}{s.StatusCodes[2].Value}{Default}, 3xx: {Yellow}{s.StatusCodes[3].Value}{Default}, 4xx: {Red}{s.StatusCodes[4].Value}{Default}, 5xx: {Red}{s.StatusCodes[5].Value}{Default}, others: {Magenta}{s.StatusCodes[0].Value}");
        }, 3, OutputPipe.Error);
    }

    private readonly struct Stats {
        public required PaddedULong CurrentCount { get; init; }
        public required PaddedULong[] StatusCodes { get; init; }
        public required double Percentage { get; init; }
        public required TimeSpan ETA { get; init; }
        public required double SuccessRate { get; init; }
        public required ulong RequestCount { get; init; }
    }

    /// <inheritdoc />
    public async Task<PulseResult> ClearAndReturnAsync() {
        // Clear after metrics
        _channel.Writer.Complete();
        await _printer.ConfigureAwait(false);
        ClearNextLines(3, OutputPipe.Error);
        System.Console.CursorVisible = true;

        return new() {
            Results = _results,
            SuccessRate = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2),
            TotalDuration = Stopwatch.GetElapsedTime(_start)
        };
    }
}
