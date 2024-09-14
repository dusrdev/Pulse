
using System.Collections.Concurrent;

using Pulse.Configuration;

namespace Pulse.Core;

public sealed class MaximumPulse : AbstractPulse {
    public MaximumPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }

    public override async Task<PulseResult> RunAsync(CancellationToken cancellationToken = default) {
        ConcurrentStack<RequestResult> stack = new();

		var tasks = Enumerable.Range(0, _config.Requests)
					.AsParallel()
					.Select(_ => _requestHandler(cancellationToken));

		await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

        return new PulseResult {
            Results = stack
        };
    }
}