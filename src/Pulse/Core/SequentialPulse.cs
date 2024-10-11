using Pulse.Configuration;

namespace Pulse.Core;

public sealed class SequentialPulse : AbstractPulse {
    public SequentialPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }


    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

        for (int i = 0; i < _parameters.Requests; i++) {
            await monitor.Observe(cancellationToken).ConfigureAwait(false);
        }

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