using Pulse.Configuration;

namespace Pulse.Core;

public sealed class LimitedPulse : AbstractPulse {
    public LimitedPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }


    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        var options = new ParallelOptions {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _parameters.ConcurrentRequests
        };

        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

        await Parallel.ForEachAsync(Enumerable.Range(0, _parameters.Requests),
                                    options,
                                    async (_, token) => await monitor.Observe(token))
                                    .ConfigureAwait(false);

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = _parameters
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(uniqueRequests!, cancellationToken);
        }
    }
}