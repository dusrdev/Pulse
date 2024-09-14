
using System.Collections.Concurrent;

using Pulse.Configuration;

namespace Pulse.Core;

public sealed class LimitedPulse : AbstractPulse {
    public LimitedPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }


    public override async Task<PulseResult> RunAsync(CancellationToken cancellationToken = default) {
        ConcurrentStack<RequestResult> stack = new();

        var options = new ParallelOptions {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _config.ConcurrentRequests
        };

        await Parallel.ForEachAsync(Enumerable.Range(0, _config.Requests),
                                    options,
                                    async (_, token) => stack.Push(await _requestHandler(token)))
                                    .ConfigureAwait(false);

        return new PulseResult {
            Results = stack
        };
    }
}