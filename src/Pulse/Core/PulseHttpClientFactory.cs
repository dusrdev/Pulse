using System.Net;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

public static class PulseHttpClientFactory {
	public static HttpClient Create(RequestDetails details) {
		var proxy = details.Proxy;

		SocketsHttpHandler handler = CreateHandler(proxy);

		return new HttpClient(handler) {
			Timeout = TimeSpan.FromMinutes(10)
		};
	}

	private static SocketsHttpHandler CreateHandler(Proxy proxyDetails) {
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
		handler.ConfigureSSLHandling(proxyDetails);
		return handler;
	}
}