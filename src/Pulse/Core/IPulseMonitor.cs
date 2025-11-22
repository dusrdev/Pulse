using System.Diagnostics;
using System.Net;
using System.Text;

using Pulse.Configuration;
using Pulse.Models;

namespace Pulse.Core;

/// <summary>
/// IPulseMonitor defines the traits for the wrappers that handles display of metrics and cross-thread data collection
/// </summary>
internal interface IPulseMonitor {
    /// <summary>
    /// Creates a new pulse monitor according the verbosity setting
    /// </summary>
    /// <param name="client"></param>
    /// <param name="requestRecipe"></param>
    /// <param name="parameters"></param>
    public static IPulseMonitor Create(HttpClient client, Request requestRecipe, Parameters parameters) {
        if (parameters.Verbose || parameters.Requests == 1) {
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
    /// Run cleanup and return results
    /// </summary>
    Task<PulseResult> ClearAndReturnAsync();

    /// <summary>
    /// Request execution context
    /// </summary>
    internal sealed class RequestExecutionContext {
        private PaddedULong _currentConcurrentConnections;

        /// <summary>
        /// Sends a request.
        /// </summary>
        /// <param name="id">The request identifier.</param>
        /// <param name="requestRecipe">The recipe used to build the request message.</param>
        /// <param name="httpClient">Configured <see cref="HttpClient"/> instance.</param>
        /// <param name="saveContent">Whether response content should be persisted.</param>
        /// <param name="cancellationToken">Cancellation token for the i/o operation.</param>
        /// <returns>A <see cref="Response"/> describing the outcome.</returns>
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
                response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            } catch (TimeoutException ex) {
                exception = StrippedException.FromException(ex);
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && ex.InnerException is TimeoutException timeoutEx) {
                exception = StrippedException.FromException(timeoutEx);
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
                    content = await r.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (contentLength == 0) {
                        var charSet = r.Content.Headers.ContentType?.CharSet;
                        var encoding = charSet is null
                            ? Encoding.UTF8
                            : Encoding.GetEncoding(charSet.Trim('"'));
                        contentLength = encoding.GetByteCount(content.AsSpan());
                    }
                }
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