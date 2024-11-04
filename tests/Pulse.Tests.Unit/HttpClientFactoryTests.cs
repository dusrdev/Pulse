using System.Net;

using Pulse.Core;

namespace Pulse.Tests.Unit;

public class HttpClientFactoryTests {
    [Fact]
    public void HttpClientFactory_WithoutProxy_ReturnsHttpClient() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var httpClient = PulseHttpClientFactory.Create(proxy);

        // Assert
        httpClient.Should().NotBeNull("because a HttpClient is returned");
    }

    [Fact]
    public void CreateHandler_WithoutProxy_ReturnsSocketsHttpHandler() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        handler.Should().NotBeNull("because a handler is returned");
    }

    [Fact]
    public void CreateHandler_WithoutProxy() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        handler.Proxy.Should().BeNull("because no proxy is configured");
    }

    [Theory]
    [InlineData("127.0.0.1:8080", "127.0.0.1:8080")]
    public void CreateHandler_WithProxy_HostOnly(string host, string expected) {
        // Arrange
        var proxy = new Proxy() {
            Bypass = false,
            Host = host,
        };

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        handler.UseProxy.Should().BeTrue("because a proxy is configured to be used");
        handler.Proxy.Should().NotBeNull("because a valid proxy should be set when UseProxy is true");

        // Create a valid destination Uri
        var destination = new Uri("http://example.com");

        // Retrieve the proxy Uri for the given destination
        var proxyUri = handler.Proxy!.GetProxy(destination);

        // Assert that the Authority (host:port) matches the expected value
        proxyUri!.Authority.Should().Be(expected, "because the proxy should point to the expected host and port");
    }

    [Fact]
    public void CreateHandler_WithProxy_WithoutPassword_NoCredentials() {
        // Arrange
        var proxy = new Proxy() {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
        };

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        handler.UseProxy.Should().BeTrue("because a proxy is configured to be used");
        handler.Proxy.Should().NotBeNull("because a valid proxy should be set when UseProxy is true");
        handler.Proxy!.Credentials.Should().BeNull("because no credentials are configured");
    }

    [Fact]
    public void CreateHandler_WithProxy_WithCredentials() {
        // Arrange
        var proxy = new Proxy() {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
            Password = "password",
        };

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        handler.UseProxy.Should().BeTrue("because a proxy is configured to be used");
        handler.Proxy.Should().NotBeNull("because a valid proxy should be set when UseProxy is true");
        var credentials = handler.Proxy!.Credentials! as NetworkCredential;
        credentials.Should().NotBeNull("because credentials are configured");
        credentials!.UserName.Should().Be(proxy.Username, "because the username matches the configured username");
        credentials!.Password.Should().Be(proxy.Password, "because the password matches the configured password");
    }
}