using Pulse.Configuration;

namespace Pulse.Core;

public sealed class SequentialPulse : AbstractPulse {
    public SequentialPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }


    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _config.Requests);

        for (int i = 0; i < _config.Requests; i++) {
            await monitor.Observe(cancellationToken).ConfigureAwait(false);
        }

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result
        };

        summary.Summarize();
    }
}