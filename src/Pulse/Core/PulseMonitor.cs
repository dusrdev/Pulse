using System.Collections.Concurrent;
using System.Diagnostics;

using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify;

using static Pulse.Core.IPulseMonitor;
using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
public sealed class PulseMonitor : IPulseMonitor {
	/// <summary>
	/// Holds the results of all the requests
	/// </summary>
	private readonly ConcurrentStack<Response> _results;

	private readonly char[] _etaBuffer = new char[30];

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
	private void PrintMetrics() {
		lock (_lock) {
			var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

			var eta = TimeSpan.FromMilliseconds(elapsed / _responses.Value * (_requestCount - (int)_responses.Value));

			double sr = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2);

			var currentLine = GetCurrentLine();
			// Clear
			ClearNextLines(2, OutputPipe.Error);
			// Line 1
			Error.Write("Completed: ");
			SetColors(Color.Yellow, Color.DefaultBackgroundColor);
			Error.Write(_responses);
			ResetColors();
			Error.Write('/');
			SetColors(Color.Yellow, Color.DefaultBackgroundColor);
			Error.Write(_requestCount);
			ResetColors();
			Error.Write(", SR: ");
			SetColors(Helper.GetPercentageBasedColor(sr), Color.DefaultBackgroundColor);
			Error.Write(sr);
			ResetColors();
			Error.Write("%, ETA: ");
			Write(Utils.DateAndTime.FormatTimeSpan(eta, _etaBuffer), OutputPipe.Error, Color.Yellow, Color.DefaultBackgroundColor);
			NewLine(OutputPipe.Error);

			// Line 2
			Error.Write("1xx: ");
			SetColors(Color.White, Color.DefaultBackgroundColor);
			Error.Write(_stats[1]);
			ResetColors();
			Error.Write(", 2xx: ");
			SetColors(Color.Green, Color.DefaultBackgroundColor);
			Error.Write(_stats[2]);
			ResetColors();
			Error.Write(", 3xx: ");
			SetColors(Color.Yellow, Color.DefaultBackgroundColor);
			Error.Write(_stats[3]);
			ResetColors();
			Error.Write(", 4xx: ");
			SetColors(Color.Red, Color.DefaultBackgroundColor);
			Error.Write(_stats[4]);
			ResetColors();
			Error.Write(", 5xx: ");
			SetColors(Color.Red, Color.DefaultBackgroundColor);
			Error.Write(_stats[5]);
			ResetColors();
			Error.Write(", others: ");
			SetColors(Color.Magenta, Color.DefaultBackgroundColor);
			Error.Write(_stats[0]);
			ResetColors();
			NewLine(OutputPipe.Error);
			// Reset location
			GoToLine(currentLine);
		}
	}

	/// <summary>
	/// Prints the initial metrics to establish ui
	/// </summary>
	private void PrintInitialMetrics() {
		lock (_lock) {
			var currentLine = GetCurrentLine();
			// Clear
			ClearNextLines(2, OutputPipe.Error);
			// Line 1
			WriteLine(["Completed: ", "0" * Color.Yellow, $"/{_requestCount}, SR: ", "0%" * Color.Red, ", ETA: ", "NaN" * Color.Yellow], OutputPipe.Error);

			// Line 2
			WriteLine(["1xx: ", "0" * Color.White, ", 2xx: ", "0" * Color.Green, ", 3xx: ", "0" * Color.Yellow, ", 4xx: ", "0" * Color.Red, ", 5xx: ", "0" * Color.Red, ", others: ", "0" * Color.Magenta], OutputPipe.Error);
			// Reset location
			GoToLine(currentLine);
		}
	}

    /// <inheritdoc />
	public PulseResult Consolidate() => new() {
		Results = _results,
		SuccessRate = Math.Round((double)_stats[2].Value / _responses.Value * 100, 2),
		TotalDuration = Stopwatch.GetElapsedTime(_start)
	};
}