using System.Collections.Concurrent;
using System.Diagnostics;

using Pulse.Configuration;

using static Pulse.Core.IPulseMonitor;

namespace Pulse.Core;


/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
public sealed class PulseMonitor : IPulseMonitor {
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
    private readonly int _requestCount;
    private readonly bool _saveContent;
    private readonly CancellationToken _cancellationToken;
    private readonly HttpClient _httpClient;
    private readonly Request _requestRecipe;

    private readonly Lock _lock = new();

    /// <summary>
    /// Creates a new pulse monitor
    /// </summary>
    public PulseMonitor(HttpClient client, Request requestRecipe, Parameters parameters) {
        _results = new ConcurrentStack<Response>();
        _requestCount = parameters.Requests;
        _saveContent = parameters.Export;
        _cancellationToken = parameters.CancellationToken;
        _httpClient = client;
        _requestRecipe = requestRecipe;
        _requestExecutionContext = new RequestExecutionContext();
        PrintInitialMetrics();
        _start = Stopwatch.GetTimestamp();
    }

    /// <inheritdoc />
    public async Task SendAsync(int requestId) {
        var result = await _requestExecutionContext.SendRequest(requestId, _requestRecipe, _httpClient, _saveContent, _cancellationToken);
        Interlocked.Increment(ref _responses.Value);
        // Increment stats

        int index = (int)result.StatusCode / 100;
        Interlocked.Increment(ref _stats[index].Value);
        // Print metrics

        PrintMetrics();
        _results.Push(result);
    }

    /// <summary>
    /// Handles printing the current metrics, has to be synchronized to prevent cross writing to the console, which produces corrupted output.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrintMetrics() {
        lock (_lock) {
            var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            var eta = TimeSpan.FromMilliseconds(elapsed / _responses.Value * (_requestCount - (int)_responses.Value));
            double sr = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2);

            var stats = new Stats {
                CurrentCount = _responses,
                RequestCount = _requestCount,
                StatusCodes = _stats,
                ETA = eta,
                SuccessRate = sr
            };

            Overwrite(stats, static s => {
                WriteLine(OutputPipe.Error, $"Completed: {Yellow}{s.CurrentCount.Value}{Default}/{Yellow}{s.RequestCount}{Default}, SR: {Helper.GetPercentageBasedColor(s.SuccessRate)}{s.SuccessRate}{Default}%, ETA: {Yellow}{s.ETA:hr}");
                WriteLine(OutputPipe.Error, $"1xx: {White}{s.StatusCodes[1].Value}{Default}, 2xx: {Green}{s.StatusCodes[2].Value}{Default}, 3xx: {Yellow}{s.StatusCodes[3].Value}{Default}, 4xx: {Red}{s.StatusCodes[4].Value}{Default}, 5xx: {Red}{s.StatusCodes[5].Value}{Default}, others: {Magenta}{s.StatusCodes[0].Value}");
            }, 2, OutputPipe.Error);
        }
    }

    private readonly ref struct Stats {
        public required PaddedULong CurrentCount { get; init; }
        public required PaddedULong[] StatusCodes { get; init; }
        public required TimeSpan ETA { get; init; }
        public required double SuccessRate { get; init; }
        public required int RequestCount { get; init; }
    }

    /// <summary>
    /// Prints the initial metrics to establish ui
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrintInitialMetrics() {
        Overwrite(_requestCount, static requests => {
            WriteLine(OutputPipe.Error, $"Completed: {Yellow}0{Default}/{Yellow}{requests}{Default}, SR: {Red}0{Default}%, ETA: {Yellow}NaN");
            WriteLine(OutputPipe.Error, $"1xx: {White}0{Default}, 2xx: {Green}0{Default}, 3xx: {Yellow}0{Default}, 4xx: {Red}0{Default}, 5xx: {Red}0{Default}, others: {Magenta}0");
        }, 2, OutputPipe.Error);
    }

    /// <inheritdoc />
    public PulseResult ClearAndReturn() {
        // Clear after metrics
        ClearNextLines(2, OutputPipe.Error);

        return new() {
            Results = _results,
            SuccessRate = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2),
            TotalDuration = Stopwatch.GetElapsedTime(_start)
        };
    }
}