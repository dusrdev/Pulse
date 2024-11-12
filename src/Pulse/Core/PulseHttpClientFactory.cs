using System.Net;

using Sharpify;

namespace Pulse.Core;

/// <summary>
/// Http client factory
/// </summary>
public static class PulseHttpClientFactory {
    /// <summary>
    /// Creates an HttpClient with the specified <paramref name="proxyDetails"/>
    /// </summary>
    /// <param name="proxyDetails"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns>An HttpClient</returns>
    public static HttpClient Create(Proxy proxyDetails, int timeoutInMs) {
		SocketsHttpHandler handler = CreateHandler(proxyDetails);

		return new HttpClient(handler) {
			Timeout = TimeSpan.FromMilliseconds(timeoutInMs)
		};
	}

	/// <summary>
	/// Creates a <see cref="SocketsHttpHandler"/> with the specified <paramref name="proxyDetails"/>
	/// </summary>
	/// <param name="proxyDetails"></param>
	/// <returns><see cref="SocketsHttpHandler"/></returns>
	internal static SocketsHttpHandler CreateHandler(Proxy proxyDetails) {
		SocketsHttpHandler handler;
		if (proxyDetails.Bypass || proxyDetails.Host.IsNullOrWhiteSpace()) {
			handler = new SocketsHttpHandler();
		} else {
			var proxy = new WebProxy(proxyDetails.Host);
			if (!proxyDetails.Username.IsNullOrWhiteSpace() && !proxyDetails.Password.IsNullOrWhiteSpace()) {
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