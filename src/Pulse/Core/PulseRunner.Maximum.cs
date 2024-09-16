using Pulse.Configuration;

namespace Pulse.Core;

public sealed class MaximumPulse : AbstractPulse {
    public MaximumPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }

    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

		var tasks = Enumerable.Range(0, _parameters.Requests)
					.AsParallel()
					.Select(async _ => await monitor.Observe(cancellationToken));

		await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

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