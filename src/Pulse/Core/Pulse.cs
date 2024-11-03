using Pulse.Configuration;

namespace Pulse.Core;

public static class Pulse {
    public static Task RunAsync(Parameters parameters, RequestDetails requestDetails) {
        if (parameters.Requests is 1 || parameters.ExecutionMode is ExecutionMode.Sequential) {
            return RunSequential(parameters, requestDetails);
        }
        return parameters.BatchSizeModified
            ? RunBounded(parameters, requestDetails)
            : RunUnbounded(parameters, requestDetails);
    }


    private static async Task RunSequential(Parameters parameters, RequestDetails requestDetails) {
        var monitor = new PulseMonitor {
            RequestCount = parameters.Requests,
            RequestRecipe = requestDetails.Request,
            HttpClient = PulseHttpClientFactory.Create(requestDetails),
            SaveContent = parameters.Export
        };

        var cancellationToken = parameters.CancellationTokenSource.Token;

        for (int i = 1; i <= parameters.Requests; i++) {
            await monitor.SendAsync(i, cancellationToken).ConfigureAwait(false);
        }

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }

    private static async Task RunBounded(Parameters parameters, RequestDetails requestDetails) {
        var monitor = new PulseMonitor {
            RequestCount = parameters.Requests,
            RequestRecipe = requestDetails.Request,
            HttpClient = PulseHttpClientFactory.Create(requestDetails),
            SaveContent = parameters.Export
        };

        var cancellationToken = parameters.CancellationTokenSource.Token;

        var totalRequests = parameters.Requests;
        var batchSize = parameters.BatchSize;
        var current = 1;
        Task[] buffer = new Task[batchSize];

        while (totalRequests > 0 && !cancellationToken.IsCancellationRequested) {
            var batch = Math.Min(batchSize, totalRequests);

            for (int i = 0; i < batch; i++) {
                buffer[i] = Task.Run(() => monitor.SendAsync(Interlocked.Increment(ref current), cancellationToken), cancellationToken);
            }

            var tasks = new ArraySegment<Task>(buffer, 0, batch);
            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

            totalRequests -= batch;
        }

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }

    private static async Task RunUnbounded(Parameters parameters, RequestDetails requestDetails) {
        var monitor = new PulseMonitor {
            RequestCount = parameters.Requests,
            RequestRecipe = requestDetails.Request,
            HttpClient = PulseHttpClientFactory.Create(requestDetails),
            SaveContent = parameters.Export
        };

        var cancellationToken = parameters.CancellationTokenSource.Token;

        var tasks = Enumerable.Range(1, parameters.Requests)
        .AsParallel()
        .Select(id => monitor.SendAsync(id, cancellationToken));

        await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = parameters
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }
}