using Pulse.Configuration;

using Sharpify.Collections;

namespace Pulse.Core;

public sealed class UnboundedPulse : AbstractPulse {
    public UnboundedPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }

    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

        if (_parameters.Requests is 1) {
            await monitor.Observe(cancellationToken);
        } else {
            using var buffer = new RentedBufferWriter<Task>(_parameters.Requests);

            for (int i = 0; i < _parameters.Requests; i++) {
                buffer.WriteAndAdvance(Task.Run(() => monitor.Observe(cancellationToken), cancellationToken));
            }

            var tasks = buffer.WrittenSegment;

            await Task.WhenAll(tasks).WaitAsync(cancellationToken);
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