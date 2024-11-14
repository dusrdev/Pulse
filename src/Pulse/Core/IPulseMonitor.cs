using System.Diagnostics;
using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// IPulseMonitor defines the traits for the wrappers that handles display of metrics and cross-thread data collection
/// </summary>
public interface IPulseMonitor {
	/// <summary>
	/// Creates a new pulse monitor according the verbosity setting
	/// </summary>
	/// <param name="client"></param>
	/// <param name="requestRecipe"></param>
	/// <param name="parameters"></param>
	public static IPulseMonitor Create(HttpClient client, Request requestRecipe, Parameters parameters) {
		if (parameters.Verbose) {
			return new VerbosePulseMonitor(client, requestRecipe, parameters);
		}
		return new PulseMonitor(client, requestRecipe, parameters);
	}

	/// <summary>
	/// Observe needs to be used instead of the execution delegate
	/// </summary>
	/// <param name="requestId"></param>
	Task SendAsync(int requestId);

	/// <summary>
	/// Consolidates the results into an object
	/// </summary>
	PulseResult Consolidate();

	/// <summary>
	/// Request execution context
	/// </summary>
	internal sealed class RequestExecutionContext {
		private volatile int _currentConcurrentConnections;

		/// <summary>
		/// Sends a request
		/// </summary>
		/// <param name="id">The request id</param>
		/// <param name="requestRecipe">The recipe for the <see cref="HttpRequestMessage"/></param>
		/// <param name="httpClient">The <see cref="HttpClient"/> to use</param>
		/// <param name="saveContent">Whether to save the content</param>
		/// <param name="cancellationToken">The cancellation token</param>
		/// <returns><see cref="Response"/></returns>
		public async Task<Response> SendRequest(int id, Request requestRecipe, HttpClient httpClient, bool saveContent, CancellationToken cancellationToken) {
			HttpStatusCode statusCode = 0;
			string content = string.Empty;
			long contentLength = 0;
			int currentConcurrencyLevel = 0;
			StrippedException exception = StrippedException.Default;
			var headers = Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
			using var message = requestRecipe.CreateMessage();
			long start = Stopwatch.GetTimestamp(), end = 0;
			try {
				currentConcurrencyLevel = Interlocked.Increment(ref _currentConcurrentConnections);
				using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				end = Stopwatch.GetTimestamp();
				Interlocked.Decrement(ref _currentConcurrentConnections);
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
				exception = new StrippedException(nameof(TimeoutException),
				$"Request {id} timeout after {elapsed.TotalMilliseconds} ms");
			} catch (Exception) {
				throw;
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
	}
}