using Pulse.Configuration;

namespace Pulse.Core;

public sealed class BoundedPulse : AbstractPulse {
    public BoundedPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }

    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

        if (_parameters.Requests is 1) {
            await monitor.Observe(cancellationToken);
        } else {
            var options = new ParallelOptions {
                MaxDegreeOfParallelism = -1,
                CancellationToken = cancellationToken
            };

            await Parallel.ForAsync(0, _parameters.Requests, options, async (_, token) => await monitor.Observe(token));
        }

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result,
            Parameters = _parameters
        };

        var (exportRequired, uniqueRequests) = summary.Summarize();

        if (exportRequired) {
            await PulseSummary.ExportUniqueRequestsAsync(uniqueRequests, cancellationToken);
        }
    }
}