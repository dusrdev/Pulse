using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Pulse.Configuration;
using static PrettyConsole.Console;
using PrettyConsole;
using static Pulse.Core.IPulseMonitor;
using System.Runtime.CompilerServices;

namespace Pulse.Core;

/// <summary>
/// PulseMonitor wraps the execution delegate and handles display of metrics and cross-thread data collection
/// </summary>
public sealed class VerbosePulseMonitor : IPulseMonitor {
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
	private volatile int _responses;

	/// <summary>
	/// Current number of successful responses received
	/// </summary>
	private volatile int _successes;

	private readonly bool _saveContent;
	private readonly CancellationToken _cancellationToken;
	private readonly HttpClient _httpClient;
	private readonly Request _requestRecipe;
	private readonly RequestExecutionContext _requestExecutionContext;

	/// <summary>
	/// Creates a new verbose pulse monitor
	/// </summary>
	public VerbosePulseMonitor(HttpClient client, Request requestRecipe, Parameters parameters) {
		_results = new ConcurrentStack<Response>();
		_saveContent = parameters.Export;
		_cancellationToken = parameters.CancellationToken;
		_httpClient = client;
		_requestRecipe = requestRecipe;
		_requestExecutionContext = new RequestExecutionContext();
		_start = Stopwatch.GetTimestamp();
	}

    /// <inheritdoc />
    public async Task SendAsync(int requestId) {
        PrintPreRequest(requestId);
		var result = await _requestExecutionContext.SendRequest(requestId, _requestRecipe, _httpClient, _saveContent, _cancellationToken);
		Interlocked.Increment(ref _responses);
		// Increment stats
		if (result.StatusCode is HttpStatusCode.OK) {
			Interlocked.Increment(ref _successes);
		}
		int status = (int)result.StatusCode;
        PrintPostRequest(requestId, status);
		_results.Push(result);
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	private static void PrintPreRequest(int requestId) {
		Error.Write("Sending request id: ");
		SetColors(Color.Yellow, Color.DefaultBackgroundColor);
		Error.WriteLine(requestId);
		ResetColors();
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	private static void PrintPostRequest(int requestId, int statusCode) {
		Error.Write("Received response id: ");
		SetColors(Color.Yellow, Color.DefaultBackgroundColor);
		Error.Write(requestId);
		ResetColors();
		Error.Write(", status code: ");
		SetColors(Helper.GetStatusCodeBasedColor(statusCode), Color.DefaultBackgroundColor);
		Error.WriteLine(statusCode);
		ResetColors();
	}

    /// <inheritdoc />
	public PulseResult Consolidate() => new() {
		Results = _results,
		SuccessRate = Math.Round((double)_successes / _responses * 100, 2),
		TotalDuration = Stopwatch.GetElapsedTime(_start)
	};
}