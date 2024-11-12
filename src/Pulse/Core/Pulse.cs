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
    public static Task RunAsync(Parameters parameters, RequestDetails requestDetails) {
        if (parameters.Requests is 1 || parameters.ExecutionMode is ExecutionMode.Sequential) {
            return RunSequential(parameters, requestDetails);
        }

        return parameters.MaxConnectionsModified
            ? RunBounded(parameters, requestDetails)
            : RunUnbounded(parameters, requestDetails);
    }

    /// <summary>
    /// Runs the pulse sequentially
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    internal static async Task RunSequential(Parameters parameters, RequestDetails requestDetails) {
        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, parameters.TimeoutInMs);

        var monitor = IPulseMonitor.Create(httpClient, requestDetails.Request, parameters);

        for (int i = 1; i <= parameters.Requests; i++) {
            await monitor.SendAsync(i).ConfigureAwait(false);
        }

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters,
            RequestSizeInBytes = requestDetails.Request.GetRequestLength()
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await summary.ExportUniqueRequestsAsync(uniqueRequests, parameters.CancellationToken);
        }
    }

    /// <summary>
    /// Runs the pulse in parallel batches
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    internal static async Task RunBounded(Parameters parameters, RequestDetails requestDetails) {
        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, parameters.TimeoutInMs);

        var cancellationToken = parameters.CancellationToken;

        var monitor = IPulseMonitor.Create(httpClient, requestDetails.Request, parameters);

        using var semaphore = new SemaphoreSlim(parameters.MaxConnections);

        var tasks = Enumerable.Range(1, parameters.Requests)
            .AsParallel()
            .Select(async requestId => {
                try {
                    await semaphore.WaitAsync(cancellationToken);
                    await monitor.SendAsync(requestId);
                } finally {
                    semaphore.Release();
                }
            });

        await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters,
            RequestSizeInBytes = requestDetails.Request.GetRequestLength()
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await summary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }

    /// <summary>
    /// Runs the pulse in parallel without any batching
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    internal static async Task RunUnbounded(Parameters parameters, RequestDetails requestDetails) {
        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, parameters.TimeoutInMs);

        var cancellationToken = parameters.CancellationToken;

        var monitor = IPulseMonitor.Create(httpClient, requestDetails.Request, parameters);

        var tasks = Enumerable.Range(1, parameters.Requests)
            .AsParallel()
            .Select(monitor.SendAsync);

        await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters,
            RequestSizeInBytes = requestDetails.Request.GetRequestLength()
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await summary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }
}