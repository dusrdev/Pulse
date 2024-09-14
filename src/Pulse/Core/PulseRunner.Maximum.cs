using Pulse.Configuration;

namespace Pulse.Core;

public sealed class MaximumPulse : AbstractPulse {
    public MaximumPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }

    public override async Task<PulseResult> RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _config.Requests);

		var tasks = Enumerable.Range(0, _config.Requests)
					.AsParallel()
					.Select(async _ => await monitor.Observe(cancellationToken));

		await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        return monitor.Consolidate();
    }
}