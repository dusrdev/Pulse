using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

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

	/// <summary>
	/// Total number of required requests
	/// </summary>
	private readonly int _requests;

	/// <summary>
	/// Timestamp of the beginning of monitoring
	/// </summary>
	private readonly long _start;

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
		_results = new();
		_handler = handler;
		_requests = requests;
		_start = Stopwatch.GetTimestamp();
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

		double eta = elapsed / _count * (_requests - _count);
		var remaining = TimeSpan.FromMilliseconds(eta).ToRemainingDuration();

		double sr = (double)_2xx / _count * 100;

		Extensions.OverrideCurrent2Lines([
		"Completed: ", $"{_count}/{_requests}" * Color.Yellow, ", SR: ", $"{sr:0.##}%" * Extensions.GetPercentageBasedColor(sr), ", ETA: ", remaining.ToString() * Color.Yellow],
		["1xx: ", _1xx.ToString() * Color.White, ", 2xx: ", _2xx.ToString() * Color.Green, ", 3xx: ", _3xx.ToString() * Color.Yellow, ", 4xx: ", _4xx.ToString() * Color.Red, ", 5xx: ", _5xx.ToString() * Color.Red, ", others: ", _others.ToString() * Color.Magenta]);
	}

	/// <summary>
	/// Consolidates the results into an object
	/// </summary>
	/// <returns></returns>
	public PulseResult Consolidate() => new() {
		Results = _results,
		TotalCount = _count,
		SuccessRate = (double)_2xx / _count * 100,
		TotalDuration = Stopwatch.GetElapsedTime(_start)
	};
}