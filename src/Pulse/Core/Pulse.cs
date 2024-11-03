using Pulse.Configuration;

using Sharpify.Collections;

namespace Pulse.Core;

public static class Pulse {
    public static Task RunAsync(Parameters parameters, RequestDetails requestDetails) => (parameters.ExecutionMode, parameters.BatchSize) switch {
				(ExecutionMode.Sequential, _) => RunSequential(parameters, requestDetails),
				#pragma warning disable IDE0002
				(ExecutionMode.Parallel, Parameters.DefaultBatchSize) => RunUnbounded(parameters, requestDetails),
				#pragma warning restore IDE0002
				(ExecutionMode.Parallel, _) => RunBounded(parameters, requestDetails),
				_ => throw new NotImplementedException()
			};

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

        if (parameters.Requests is 1) {
            await monitor.SendAsync(1, cancellationToken);
        } else {
            var totalRequests = parameters.Requests;
            var batchSize = parameters.BatchSize;
            var current = 1;

            while (totalRequests > 0 && !cancellationToken.IsCancellationRequested) {
                var batch = Math.Min(batchSize, totalRequests);

                using var buffer = new RentedBufferWriter<Task>(batch);

                for (int i = 0; i < batch; i++) {
                    buffer.WriteAndAdvance(Task.Run(() => monitor.SendAsync(current, cancellationToken), cancellationToken));
                    current++;
                }

                var tasks = buffer.WrittenSegment;
                await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

                totalRequests -= batch;
            }
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

        if (parameters.Requests is 1) {
            await monitor.SendAsync(1, cancellationToken);
        } else {
            using var buffer = new RentedBufferWriter<Task>(parameters.Requests);

            for (int i = 1; i <= parameters.Requests; i++) {
                buffer.WriteAndAdvance(Task.Run(() => monitor.SendAsync(i, cancellationToken), cancellationToken));
            }

            var tasks = buffer.WrittenSegment;

            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
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
}