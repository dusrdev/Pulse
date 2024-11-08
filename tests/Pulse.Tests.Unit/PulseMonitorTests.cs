using Pulse.Core;

namespace Pulse.Tests.Unit;

public class PulseMonitorTests {
    [Fact]
    public async Task SendAsync_ReturnsTimeoutException_OnTimeout() {
        // Arrange
        var requestDetails = new RequestDetails() {
            Proxy = new Proxy(),
            Request = new Request() {
                Url = "https://google.com",
                Method = HttpMethod.Get
            }
        };

        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, 50);

        // Act + Assert
        var result = await PulseMonitor.SendRequest(1, requestDetails.Request, httpClient, false, CancellationToken.None);
        result.Exception.Type.Should().Be(nameof(TimeoutException));
    }
}