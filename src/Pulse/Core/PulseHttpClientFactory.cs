using System.Net;

namespace Pulse.Core;

/// <summary>
/// Http client factory
/// </summary>
internal static class PulseHttpClientFactory {
    /// <summary>
    /// Creates an HttpClient with the specified <paramref name="proxyDetails"/>
	/// </summary>
	/// <param name="proxyDetails"></param>
	/// <param name="timeoutInMs"></param>
	/// <returns>An HttpClient</returns>
	public static HttpClient Create(Proxy proxyDetails, int timeoutInMs) {
#pragma warning disable CA2000 // Dispose objects before losing scope
        SocketsHttpHandler handler = CreateHandler(proxyDetails);
#pragma warning restore CA2000 // Dispose objects before losing scope

        var client = new HttpClient(handler, true) {
            Timeout = timeoutInMs < 0
                ? Timeout.InfiniteTimeSpan
                : TimeSpan.FromMilliseconds(timeoutInMs)
        };

        return client;
    }

    /// <summary>
    /// Creates a <see cref="SocketsHttpHandler"/> with the specified <paramref name="proxyDetails"/>
    /// </summary>
    /// <param name="proxyDetails"></param>
    /// <returns><see cref="SocketsHttpHandler"/></returns>
    internal static SocketsHttpHandler CreateHandler(Proxy proxyDetails) {
        SocketsHttpHandler handler;
        if (proxyDetails.Bypass || proxyDetails.Host is null or { Length: 0 }) {
            handler = new SocketsHttpHandler();
        } else {
            var proxy = new WebProxy(proxyDetails.Host);
            if (proxyDetails.Username.Length > 0 && proxyDetails.Password.Length > 0) {
                proxy.Credentials = new NetworkCredential {
                    UserName = proxyDetails.Username,
                    Password = proxyDetails.Password
                };
            }
            handler = new SocketsHttpHandler() {
                UseProxy = true,
                Proxy = proxy
            };
        }
        handler.ConfigureSslHandling(proxyDetails);
        return handler;
    }
}