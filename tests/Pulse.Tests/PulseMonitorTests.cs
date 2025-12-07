using Pulse.Core;
using Pulse.Models;

namespace Pulse.Tests;

public class PulseMonitorTests {
    [Test]
    public async Task SendAsync_ReturnsTimeoutException_OnTimeout() {
        var requestDetails = new RequestDetails {
            Proxy = new Proxy(),
            Request = new Request {
                Url = "https://google.com",
                Method = HttpMethod.Get
            }
        };

        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, 50);
        var parameters = new Parameters(new ParametersBase {
            Requests = 1,
            Export = false,
            Quiet = true
        }, CancellationToken.None);
        var monitor = new Core.Pulse(httpClient, requestDetails.Request, parameters);

        var result = await monitor.SendRequest(1, requestDetails.Request, httpClient, false, CancellationToken.None);
        await Assert.That(result.Exception.Type).IsEqualTo(nameof(TimeoutException));
    }
}