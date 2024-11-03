using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify;
using System.Runtime.CompilerServices;
using Pulse.Configuration;
using System.Net.Http.Headers;

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
	/// The memory used before the operation has begun
	/// </summary>
	private readonly long _startingWorkingSet;

	/// <summary>
	/// Current number of requests processed
	/// </summary>
	private volatile int _count;

	// response status code counter
	// 0: exception
	// 1: 1xx
	// 2: 2xx
	// 3: 3xx
	// 4: 4xx
	// 5: 5xx
	private readonly int[] _stats = new int[6];

	private volatile int _isYielding;

	private readonly TaskCompletionSource _loadingTaskSource;
	private readonly Task _indeterminateProgressBarTask;

	public required int RequestCount { get; init; }
	public required Request RequestRecipe { get; init; }
	public required HttpClient HttpClient { get; init; }
	public required bool SaveContent { get; init; }

	/// <summary>
	/// Creates a new pulse monitor
	/// </summary>
	/// <param name="handler">request delegate</param>
	/// <param name="requests">total number of required requests</param>
	public PulseMonitor() {
		_startingWorkingSet = Environment.WorkingSet;
		_results = new();
		_start = Stopwatch.GetTimestamp();
		_loadingTaskSource = new();
		var globalTCS = Services.Shared.Parameters.CancellationTokenSource;
		globalTCS.Token.Register(() => _loadingTaskSource.TrySetCanceled());
		var indeterminateProgressBar = new IndeterminateProgressBar() {
			DisplayElapsedTime = true,
			UpdateRate = 100,
		};
		_indeterminateProgressBarTask = indeterminateProgressBar.RunAsync(_loadingTaskSource.Task, globalTCS.Token);
	}

	/// <summary>
	/// Observe needs to be used instead of the execution delegate
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task SendAsync(int requestId, CancellationToken cancellationToken = default) {
		var result = await SendRequest(requestId, RequestRecipe, HttpClient, SaveContent, cancellationToken);
		Interlocked.Increment(ref _count);
		if (Interlocked.CompareExchange(ref _isYielding, 1, 0) == 0) {
			_loadingTaskSource.TrySetResult();
			await _indeterminateProgressBarTask;
		}
		// Increment stats
		int index = (int)result.StatusCode / 100;
		Interlocked.Increment(ref _stats[index]);
		// Print metrics
		PrintMetrics();
		_results.Push(result);
	}

	private static async Task<Response> SendRequest(int id, Request requestRecipe, HttpClient httpClient, bool saveContent, CancellationToken cancellationToken = default) {
		HttpStatusCode statusCode = 0;
		string content = "";
		Exception? exception = null;
		HttpResponseHeaders? headers = null;
		int threadId = 0;
		using var message = requestRecipe.CreateMessage();
		var start = Stopwatch.GetTimestamp();
		try {
			threadId = Environment.CurrentManagedThreadId;
			using var response = await httpClient.SendAsync(message, cancellationToken);
			statusCode = response.StatusCode;
			headers = response.Headers;
			if (saveContent) {
				content = await response.Content.ReadAsStringAsync(cancellationToken);
			}
		} catch (Exception e) {
			exception = e;
		} finally {
			message?.Dispose();
		}
		TimeSpan duration = Stopwatch.GetElapsedTime(start);
		return new Response {
			Id = id,
			StatusCode = statusCode,
			Headers = headers,
			Content = content,
			Duration = duration,
			Exception = exception is null ? StrippedException.Default : new StrippedException(exception),
			ExecutingThreadId = threadId
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
	/// Consolidates the results into an object
	/// </summary>
	/// <returns></returns>
	public PulseResult Consolidate() => new() {
		Results = _results,
		TotalCount = _count,
		SuccessRate = Math.Round((double)_stats[2] / _count * 100, 2),
		TotalDuration = Stopwatch.GetElapsedTime(_start),
		MemoryUsed = Environment.WorkingSet - _startingWorkingSet
	};
}