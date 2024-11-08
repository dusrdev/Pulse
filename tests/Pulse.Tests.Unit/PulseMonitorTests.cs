using Pulse.Configuration;

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
        var pBase = ParametersBase.Default with {
            TimeoutInMs = 50,
            Export = false
        };
        var parameters = new Parameters(pBase, CancellationToken.None);

        using var httpClient = PulseHttpClientFactory.Create(requestDetails.Proxy, parameters.TimeoutInMs);

        var monitor = new PulseMonitor {
            RequestCount = parameters.Requests,
            RequestRecipe = requestDetails.Request,
            HttpClient = httpClient,
            SaveContent = parameters.Export,
            CancellationToken = parameters.CancellationToken
        };

        // Act + Assert
        await monitor.SendAsync(1);
        var result = monitor.Consolidate();
        result.Results.Should().HaveCount(1);
        result.Results.TryPop(out var first).Should().BeTrue();
        first.Exception.Type.Should().Be(nameof(TimeoutException));
    }
}