using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify;

namespace Pulse.Core;

public sealed class PulseMonitor {
	private readonly ConcurrentStack<RequestResult> _results;
	private readonly Func<CancellationToken, Task<RequestResult>> _handler;
	private readonly int _requests;
	private readonly long _start;

	private volatile int _count;
	private volatile int _1xx;
	private volatile int _2xx;
	private volatile int _3xx;
	private volatile int _4xx;
	private volatile int _5xx;
	private volatile int _others;

	public PulseMonitor(Func<CancellationToken, Task<RequestResult>> handler, int requests) {
		_results = new();
		_handler = handler;
		_requests = requests;
		_start = Stopwatch.GetTimestamp();
	}

	public async Task Observe(CancellationToken cancellationToken = default) {
		var result = await _handler(cancellationToken);
		Interlocked.Increment(ref _count);
		IncrementStats(result.StatusCode);
		PrintMetrics();
		_results.Push(result);
	}

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

	private void PrintMetrics() {
		var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

		double eta = elapsed / _count * (_requests - _count);
		var remaining = TimeSpan.FromMilliseconds(eta).ToRemainingDuration();

		double sr = (double)_2xx / _count * 100;

		OverrideCurrentLine(
		$"""
		Completed: {_count}/{_requests}, SR: {sr:##.00}%, ETA: {remaining}
		1xx: {_1xx}, 2xx: {_2xx}, 3xx: {_3xx}, 4xx: {_4xx}, 5xx: {_5xx}, others: {_others}
		""" * Color.Yellow);
	}

	public PulseResult Consolidate() => new() {
		Results = _results,
		TotalCount = _count,
		SuccessRate = (double)_2xx / _count * 100,
		TotalDuration = Stopwatch.GetElapsedTime(_start)
	};
}