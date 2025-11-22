using Pulse.Configuration;
using Pulse.Models;

namespace Pulse.Core;

/// <summary>
/// Pulse runner
/// </summary>
internal static class Pulse {
    /// <summary>
    /// Runs the pulse according the specification requested in <paramref name="parameters"/>
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    public static async Task RunAsync(Parameters parameters, RequestDetails requestDetails) {
        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, parameters.TimeoutInMs);

        var cancellationToken = parameters.CancellationToken;

        var monitor = IPulseMonitor.Create(httpClient, requestDetails.Request, parameters);

        // If connections is not modified it will be set to the number of requests
        // so that all requests are sent in parallel by default.
        int concurrencyLevel = Math.Max(1, parameters.Connections);
        int totalRequests = parameters.Requests;
        int workerCount = totalRequests == 0 ? 0 : Math.Min(concurrencyLevel, totalRequests);

        var workers = new Task[workerCount];
        int nextRequestId = 0;

        for (int i = 0; i < workers.Length; i++) {
            workers[i] = Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested) {
                    int requestId = Interlocked.Increment(ref nextRequestId);
                    if (requestId > totalRequests) {
                        break;
                    }

                    await monitor.SendAsync(requestId).ConfigureAwait(false);
                    if (parameters.DelayInMs > 0) {
                        await Task.Delay(parameters.DelayInMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }, cancellationToken);
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        var result = await monitor.ClearAndReturnAsync().ConfigureAwait(false);

        var (exportRequired, uniqueRequests) = PulseSummary.Summarize(parameters, result, requestDetails.Request.GetRequestLength());

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(parameters, uniqueRequests, cancellationToken).ConfigureAwait(false);
        }
    }
}
