using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify;
using System.Runtime.CompilerServices;

namespace Pulse.Core;

/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
public sealed class PulseMonitor {
	/// <summary>
	/// Holds the results of all the requests
	/// </summary>
	private readonly ConcurrentStack<Response> _results;

	/// <summary>
	/// The handler used to run a request
	/// </summary>
	private readonly Func<CancellationToken, Task<Response>> _handler;

	private readonly char[] _etaBuffer = new char[30];

	/// <summary>
	/// Total number of required requests
	/// </summary>
	private readonly int _requests;

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

	// Number of 1xx responses
	private volatile int _1xx;
	// Number of 2xx responses
	private volatile int _2xx;
	// Number of 3xx responses
	private volatile int _3xx;
	// Number of 4xx responses
	private volatile int _4xx;
	// Number of 5xx responses
	private volatile int _5xx;
	// Number of other responses
	private volatile int _others;

	/// <summary>
	/// Creates a new pulse monitor
	/// </summary>
	/// <param name="handler">request delegate</param>
	/// <param name="requests">total number of required requests</param>
	public PulseMonitor(Func<CancellationToken, Task<Response>> handler, int requests) {
		_startingWorkingSet = Environment.WorkingSet;
		_results = new();
		_handler = handler;
		_requests = requests;
		_start = Stopwatch.GetTimestamp();
		var cursorTop = System.Console.CursorTop;
		WriteLineError("Executing..." * Color.White);
		System.Console.SetCursorPosition(0, cursorTop);
	}

	/// <summary>
	/// Observe needs to be used instead of the execution delegate
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task Observe(CancellationToken cancellationToken = default) {
		var result = await _handler(cancellationToken);
		Interlocked.Increment(ref _count);
		IncrementStats(result.StatusCode);
		PrintMetrics();
		_results.Push(result);
	}

	/// <summary>
	/// Increments the different status code counters
	/// </summary>
	/// <param name="statusCode"></param>
	private void IncrementStats(HttpStatusCode? statusCode) {
		if (statusCode is null) {
			Interlocked.Increment(ref _others);
			return;
		}

		int numeric = (int)statusCode;

		if (numeric < 200) {
			Interlocked.Increment(ref _1xx);
		} else if (numeric < 300) {
			Interlocked.Increment(ref _2xx);
		} else if (numeric < 400) {
			Interlocked.Increment(ref _3xx);
		} else if (numeric < 500) {
			Interlocked.Increment(ref _4xx);
		} else {
			Interlocked.Increment(ref _5xx);
		}
	}

	/// <summary>
	/// Handles printing the current metrics, has to be synchronized to prevent cross writing to the console, which produces corrupted output.
	/// </summary>
	[MethodImpl(MethodImplOptions.Synchronized)]
	private void PrintMetrics() {
		var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

		var eta = TimeSpan.FromMilliseconds(elapsed / _count * (_requests - _count));

		double sr = Math.Round((double)_2xx / _count * 100, 2);

		var cursor = System.Console.CursorTop;
		// Clear
		ClearNextLinesError(2);
		// Line 1
		Error.Write("Completed: ");
		WriteError(_count, Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write('/');
		WriteError(_requests, Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write(", SR: ");
		WriteError(sr, Extensions.GetPercentageBasedColor(sr), Color.DefaultBackgroundColor);
		Error.Write("%, ETA: ");
		WriteError(Utils.DateAndTime.FormatTimeSpan(eta, _etaBuffer), Color.Yellow, Color.DefaultBackgroundColor);
		Error.WriteLine();

		// Line 2
		Error.Write("1xx: ");
		WriteError(_1xx, Color.White, Color.DefaultBackgroundColor);
		Error.Write(", 2xx: ");
		WriteError(_2xx, Color.Green, Color.DefaultBackgroundColor);
		Error.Write(", 3xx: ");
		WriteError(_3xx, Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write(", 4xx: ");
		WriteError(_4xx, Color.Red, Color.DefaultBackgroundColor);
		Error.Write(", 5xx: ");
		WriteError(_5xx, Color.Red, Color.DefaultBackgroundColor);
		Error.Write(", others: ");
		WriteError(_others, Color.Magenta, Color.DefaultBackgroundColor);
		Error.WriteLine();
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
		SuccessRate = Math.Round((double)_2xx / _count * 100, 2),
		TotalDuration = Stopwatch.GetElapsedTime(_start),
		MemoryUsed = Environment.WorkingSet - _startingWorkingSet
	};
}