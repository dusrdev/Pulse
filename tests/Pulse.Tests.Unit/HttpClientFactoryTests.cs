using System.Net;

using Pulse.Configuration;

using Pulse.Core;

namespace Pulse.Tests.Unit;

public class HttpClientFactoryTests {
    [Fact]
    public void HttpClientFactory_DefaultTimeout_IsInfinite() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var httpClient = PulseHttpClientFactory.Create(proxy, -1);

        // Assert
        Assert.Equal(Timeout.InfiniteTimeSpan, httpClient.Timeout);
    }

    [Fact]
    public void HttpClientFactory_WithoutProxy_ReturnsHttpClient() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var httpClient = PulseHttpClientFactory.Create(proxy, -1);

        // Assert
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void CreateHandler_WithoutProxy_ReturnsSocketsHttpHandler() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void CreateHandler_WithoutProxy() {
        // Arrange
        var proxy = new Proxy();

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        Assert.Null(handler.Proxy);
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
        Assert.True(handler.UseProxy);
        Assert.NotNull(handler.Proxy);

        // Create a valid destination Uri
        var destination = new Uri("http://example.com");

        // Retrieve the proxy Uri for the given destination
        var proxyUri = handler.Proxy!.GetProxy(destination);

        // Assert that the Authority (host:port) matches the expected value
        Assert.Equal(expected, proxyUri!.Authority);
    }

    [Fact]
    public void CreateHandler_WithProxy_WithoutPassword_NoCredentials() {
        // Arrange
        var proxy = new Proxy {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
        };

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        Assert.True(handler.UseProxy);
        Assert.NotNull(handler.Proxy);
        Assert.Null(handler.Proxy!.Credentials);
    }

    [Fact]
    public void CreateHandler_WithProxy_WithCredentials() {
        // Arrange
        var proxy = new Proxy {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
            Password = "password",
        };

        // Act
        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        // Assert
        Assert.True(handler.UseProxy);
        Assert.NotNull(handler.Proxy);
        Assert.NotNull(handler.Proxy!.Credentials);
        var credentials = handler.Proxy!.Credentials! as NetworkCredential;
        Assert.NotNull(credentials);
        Assert.Equal(proxy.Username, credentials.UserName);
        Assert.Equal(proxy.Password, credentials.Password);
    }
}