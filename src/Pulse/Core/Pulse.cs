using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// Pulse runner
/// </summary>
public static class Pulse {
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

        using var semaphore = new SemaphoreSlim(concurrencyLevel, concurrencyLevel);

        var tasks = new Task[parameters.Requests];

        for (int i = 0; i < tasks.Length; i++) {
            var requestId = i + 1;
            tasks[i] = Task.Run(async () => {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    await monitor.SendAsync(requestId).ConfigureAwait(false);
                    if (parameters.DelayInMs > 0) {
                        await Task.Delay(parameters.DelayInMs, cancellationToken).ConfigureAwait(false);
                    }
                } finally {
                    semaphore.Release();
                }
            }, cancellationToken);
        }

        await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        var result = monitor.ClearAndReturn();

        var (exportRequired, uniqueRequests) = PulseSummary.Summarize(parameters, result, requestDetails.Request.GetRequestLength());

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(parameters, uniqueRequests, cancellationToken);
        }
    }
}