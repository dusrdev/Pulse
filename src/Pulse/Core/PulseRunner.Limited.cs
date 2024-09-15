using Pulse.Configuration;

namespace Pulse.Core;

public sealed class LimitedPulse : AbstractPulse {
    public LimitedPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }


    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        var options = new ParallelOptions {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _config.ConcurrentRequests
        };

        PulseMonitor monitor = new(_requestHandler, _config.Requests);

        await Parallel.ForEachAsync(Enumerable.Range(0, _config.Requests),
                                    options,
                                    async (_, token) => await monitor.Observe(token))
                                    .ConfigureAwait(false);

        var result = monitor.Consolidate();

        var summary = new PulseSummary {
            Result = result
        };

        summary.Summarize();
    }
}