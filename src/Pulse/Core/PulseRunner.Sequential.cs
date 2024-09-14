
using System.Collections.Concurrent;

using Pulse.Configuration;

namespace Pulse.Core;

public sealed class SequentialPulse : AbstractPulse {
    public SequentialPulse(Config config, RequestDetails requestDetails) : base(config, requestDetails) {
    }


    public override async Task<PulseResult> RunAsync(CancellationToken cancellationToken = default) {
        ConcurrentStack<RequestResult> stack = new();

        for (int i = 0; i < _config.Requests; i++) {
            stack.Push(await _requestHandler(cancellationToken).ConfigureAwait(false));
        }

        return new PulseResult {
            Results = stack
        };
    }
}