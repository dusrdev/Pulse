using System.Net;

using Pulse.Core;
using Pulse.Models;

namespace Pulse.Tests;

public class HttpClientFactoryTests {
    [Test]
    public async Task HttpClientFactory_DefaultTimeout_IsInfinite() {
        var proxy = new Proxy();

        using var httpClient = PulseHttpClientFactory.Create(proxy, -1);

        await Assert.That(httpClient.Timeout).IsEqualTo(Timeout.InfiniteTimeSpan);
    }

    [Test]
    public async Task HttpClientFactory_WithoutProxy_ReturnsHttpClient() {
        var proxy = new Proxy();

        using var httpClient = PulseHttpClientFactory.Create(proxy, -1);

        await Assert.That(httpClient).IsNotNull();
    }

    [Test]
    public async Task CreateHandler_WithoutProxy_ReturnsSocketsHttpHandler() {
        var proxy = new Proxy();

        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        await Assert.That(handler).IsNotNull();
    }

    [Test]
    public async Task CreateHandler_WithoutProxy() {
        var proxy = new Proxy();

        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        await Assert.That(handler.Proxy).IsNull();
    }

    [Test]
    [Arguments("127.0.0.1:8080", "127.0.0.1:8080")]
    public async Task CreateHandler_WithProxy_HostOnly(string host, string expected) {
        var proxy = new Proxy {
            Bypass = false,
            Host = host,
        };

        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        await Assert.That(handler.UseProxy).IsTrue();
        await Assert.That(handler.Proxy).IsNotNull();

        var destination = new Uri("http://example.com");
        var proxyUri = handler.Proxy!.GetProxy(destination);

        await Assert.That(proxyUri!.Authority).IsEqualTo(expected);
    }

    [Test]
    public async Task CreateHandler_WithProxy_WithoutPassword_NoCredentials() {
        var proxy = new Proxy {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
        };

        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        await Assert.That(handler.UseProxy).IsTrue();
        await Assert.That(handler.Proxy).IsNotNull();
        await Assert.That(handler.Proxy!.Credentials).IsNull();
    }

    [Test]
    public async Task CreateHandler_WithProxy_WithCredentials() {
        var proxy = new Proxy {
            Bypass = false,
            Host = "127.0.0.1:8080",
            Username = "username",
            Password = "password",
        };

        using var handler = PulseHttpClientFactory.CreateHandler(proxy);

        await Assert.That(handler.UseProxy).IsTrue();
        await Assert.That(handler.Proxy).IsNotNull();
        await Assert.That(handler.Proxy!.Credentials).IsNotNull();
        var credentials = handler.Proxy!.Credentials as NetworkCredential;
        await Assert.That(credentials).IsNotNull();
        await Assert.That(credentials!.UserName).IsEqualTo(proxy.Username);
        await Assert.That(credentials.Password).IsEqualTo(proxy.Password);
    }
}
