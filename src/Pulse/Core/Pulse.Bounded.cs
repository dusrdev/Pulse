using Pulse.Configuration;

using Sharpify.Collections;

namespace Pulse.Core;

public sealed class BoundedPulse : AbstractPulse {
    public BoundedPulse(Parameters parameters, RequestDetails requestDetails) : base(parameters, requestDetails) {
    }

    public override async Task RunAsync(CancellationToken cancellationToken = default) {
        PulseMonitor monitor = new(_requestHandler, _parameters.Requests);

        const int batchSize = 5; // change to use from config

        if (_parameters.Requests is 1) {
            await monitor.Observe(cancellationToken);
        } else {
            var totalRequests = _parameters.Requests;

            while (totalRequests > 0 && !cancellationToken.IsCancellationRequested) {
                var batch = Math.Min(batchSize, totalRequests);

                using var buffer = new RentedBufferWriter<Task>(batch);

                for (int i = 0; i < batch; i++) {
                    buffer.WriteAndAdvance(Task.Run(() => monitor.Observe(cancellationToken), cancellationToken));
                }

                var tasks = buffer.WrittenSegment;
                await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);

                totalRequests -= batch;
            }
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