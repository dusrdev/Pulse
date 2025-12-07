using System.Diagnostics;
using System.Net;
using System.Text;

using Pulse.Models;

namespace Pulse.Core;

/// <summary>
/// Executes individual HTTP requests while tracking concurrency and capturing results.
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
        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
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
