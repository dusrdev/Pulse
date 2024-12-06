using System.Diagnostics;
using System.Net;
using System.Text;

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
		private PaddedULong _currentConcurrentConnections;

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
			long start = Stopwatch.GetTimestamp();
			TimeSpan elapsed = TimeSpan.Zero;
			HttpResponseMessage? response = null;
			try {
				currentConcurrencyLevel = (int)Interlocked.Increment(ref _currentConcurrentConnections.Value);
				response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
				if (cancellationToken.IsCancellationRequested) {
					throw;
				}
				exception = new StrippedException
				(nameof(TimeoutException), $"Request {id} interrupted by manual timeout");
			} catch (Exception) {
				throw;
			} finally {
				Interlocked.Decrement(ref _currentConcurrentConnections.Value);
			}
			elapsed = Stopwatch.GetElapsedTime(start);
			if (!exception.IsDefault) {
				return new Response {
					Id = id,
					StatusCode = statusCode,
					Headers = headers,
					Content = content,
					ContentLength = contentLength,
					Latency = elapsed,
					Exception = exception,
					CurrentConcurrentConnections = currentConcurrencyLevel
				};
			}

			try {
				var r = response!;
				statusCode = r.StatusCode;
				headers = r.Headers;
				var length = r.Content.Headers.ContentLength;
				if (length.HasValue) {
					contentLength = length.Value;
				}
				if (saveContent) {
					content = await r.Content.ReadAsStringAsync(cancellationToken);
					if (contentLength == 0) {
						var charSet = r.Content.Headers.ContentType?.CharSet;
						var encoding = charSet is null
								? Encoding.UTF8 // doesn't exist - fallback to UTF8
								: Encoding.GetEncoding(charSet.Trim('"')); // exist - use server's
						contentLength = encoding.GetByteCount(content.AsSpan());
					}
				}
			} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
				if (cancellationToken.IsCancellationRequested) {
					throw;
				}
				exception = new StrippedException
				(nameof(TimeoutException), $"Reading request {id} content interrupted by manual timeout");
			} catch (Exception) {
				throw;
			} finally {
				response?.Dispose();
			}
			return new Response {
				Id = id,
				StatusCode = statusCode,
				Headers = headers,
				Content = content,
				ContentLength = contentLength,
				Latency = elapsed,
				Exception = exception,
				CurrentConcurrentConnections = currentConcurrencyLevel
			};
		}
	}
}