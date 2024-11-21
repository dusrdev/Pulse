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
            await Task.Delay(parameters.DelayInMs);
            await monitor.SendAsync(i);
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

        var tasks = new Task[parameters.Requests];

        for (int i = 0; i < parameters.Requests; i++) {
            await semaphore.WaitAsync(cancellationToken);

#pragma warning disable IDE0053 // Use expression body for lambda expression
            // lambda expression will change return type
            tasks[i] = monitor.SendAsync(i + 1).ContinueWith(_ => {
                semaphore.Release();
            });
#pragma warning restore IDE0053 // Use expression body for lambda expression

        }

        await Task.WhenAll(tasks.AsSpan()).WaitAsync(cancellationToken).ConfigureAwait(false);

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

        var tasks = new Task[parameters.Requests];

        for (int i = 0; i < parameters.Requests; i++) {
            tasks[i] = monitor.SendAsync(i + 1);
        }

        await Task.WhenAll(tasks.AsSpan()).WaitAsync(cancellationToken).ConfigureAwait(false);

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