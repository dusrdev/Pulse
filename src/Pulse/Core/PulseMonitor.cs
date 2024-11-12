using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify;
using System.Runtime.CompilerServices;
using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
public sealed class PulseMonitor {
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
	/// Current number of requests processed
	/// </summary>
	private volatile int _count;

	/// <summary>
	/// Current concurrency level
	/// </summary>
	private static volatile int _concurrencyLevel;

	// response status code counter
	// 0: exception
	// 1: 1xx
	// 2: 2xx
	// 3: 3xx
	// 4: 4xx
	// 5: 5xx
	private readonly int[] _stats = new int[6];
	public required int RequestCount { get; init; }
	public required Request RequestRecipe { get; init; }
	public required HttpClient HttpClient { get; init; }
	public required CancellationToken CancellationToken { get; init; }
	public required bool SaveContent { get; init; }

	/// <summary>
	/// Creates a new pulse monitor
	/// </summary>
	/// <param name="handler">request delegate</param>
	/// <param name="requests">total number of required requests</param>
	public PulseMonitor() {
		_results = new();
		_start = Stopwatch.GetTimestamp();
		PrintInitialMetrics();
	}

	/// <summary>
	/// Observe needs to be used instead of the execution delegate
	/// </summary>
	/// <param name="cancellationToken"></param>
	public async Task SendAsync(int requestId) {
		var result = await SendRequest(requestId, RequestRecipe, HttpClient, SaveContent, CancellationToken);
		Interlocked.Increment(ref _count);
		// Increment stats
		int index = (int)result.StatusCode / 100;
		Interlocked.Increment(ref _stats[index]);
		// Print metrics
		PrintMetrics();
		_results.Push(result);
	}

	/// <summary>
	/// Sends a request
	/// </summary>
	/// <param name="id">The request id</param>
	/// <param name="requestRecipe">The recipe for the <see cref="HttpRequestMessage"/></param>
	/// <param name="httpClient">The <see cref="HttpClient"/> to use</param>
	/// <param name="saveContent">Whether to save the content</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <returns><see cref="Response"/></returns>
	internal static async Task<Response> SendRequest(int id, Request requestRecipe, HttpClient httpClient, bool saveContent, CancellationToken cancellationToken = default) {
		HttpStatusCode statusCode = 0;
		string content = "";
		long contentLength = 0;
		int currentConcurrencyLevel = 0;
		StrippedException exception = StrippedException.Default;
		var headers = Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
		using var message = requestRecipe.CreateMessage();
		long start = Stopwatch.GetTimestamp(), end = 0;
		try {
			currentConcurrencyLevel = Interlocked.Increment(ref _concurrencyLevel);
			using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			end = Stopwatch.GetTimestamp();
			Interlocked.Decrement(ref _concurrencyLevel);
			statusCode = response.StatusCode;
			headers = response.Headers;
			var length = response.Content.Headers.ContentLength;
			if (length.HasValue) {
				contentLength = length.Value;
			}
			if (saveContent) {
				content = await response.Content.ReadAsStringAsync(cancellationToken);
			}
		} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
			if (cancellationToken.IsCancellationRequested) {
				throw;
			}
			var elapsed = Stopwatch.GetElapsedTime(start);
			exception = new StrippedException(nameof(TimeoutException), $"Request {id} timeout after {elapsed.TotalMilliseconds} ms", "");
		} catch (Exception e) {
			end = Stopwatch.GetTimestamp();
			exception = StrippedException.FromException(e);
		} finally {
			message?.Dispose();
		}
		return new Response {
			Id = id,
			StatusCode = statusCode,
			Headers = headers,
			Content = content,
			ContentLength = contentLength,
			Latency = Stopwatch.GetElapsedTime(start, end),
			Exception = exception,
			CurrentConcurrentConnections = currentConcurrencyLevel
		};
	}

	/// <summary>
	/// Handles printing the current metrics, has to be synchronized to prevent cross writing to the console, which produces corrupted output.
	/// </summary>
	[MethodImpl(MethodImplOptions.Synchronized)]
	private void PrintMetrics() {
		var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

		var eta = TimeSpan.FromMilliseconds(elapsed / _count * (RequestCount - _count));

		double sr = Math.Round((double)_stats[2] / _count * 100, 2);

		var cursor = System.Console.CursorTop;
		// Clear
		ClearNextLinesError(2);
		// Line 1
		Error.Write("Completed: ");
		SetColors(Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write(_count);
		ResetColors();
		Error.Write('/');
		SetColors(Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write(RequestCount);
		ResetColors();
		Error.Write(", SR: ");
		SetColors(Extensions.GetPercentageBasedColor(sr), Color.DefaultBackgroundColor);
		Error.Write(sr);
		ResetColors();
		Error.Write("%, ETA: ");
		WriteError(Utils.DateAndTime.FormatTimeSpan(eta, _etaBuffer), Color.Yellow, Color.DefaultBackgroundColor);
		NewLineError();

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
		NewLineError();
		// Reset location
		System.Console.SetCursorPosition(0, cursor);
	}

	/// <summary>
	/// Prints the initial metrics to establish ui
	/// </summary>
	[MethodImpl(MethodImplOptions.Synchronized)]
	private void PrintInitialMetrics() {
		var cursor = System.Console.CursorTop;
		// Clear
		ClearNextLinesError(2);
		// Line 1
		WriteLineError(["Completed: ", "0" * Color.Yellow, $"/{RequestCount}, SR: ", "0%" * Color.Red, ", ETA: ", "NaN" * Color.Yellow]);

		// Line 2
		WriteLineError(["1xx: ", "0" * Color.White, ", 2xx: ", "0" * Color.Green, ", 3xx: ", "0" * Color.Yellow, ", 4xx: ", "0" * Color.Red, ", 5xx: ", "0" * Color.Red, ", others: ", "0" * Color.Magenta]);
		// Reset location
		System.Console.SetCursorPosition(0, cursor);
	}

	/// <summary>
	/// Consolidates the results into an object
	/// </summary>
	/// <returns></returns>
	public PulseResult Consolidate() => new() {
		Results = _results,
		SuccessRate = Math.Round((double)_stats[2] / _count * 100, 2),
		TotalDuration = Stopwatch.GetElapsedTime(_start)
	};
}