using System.Net;

namespace Pulse.Core;

public static class PulseHttpClientFactory {
	public static HttpClient Create(RequestDetails details) {
		var proxy = details.Proxy;

		SocketsHttpHandler handler = CreateHandler(proxy);

		return new HttpClient(handler);
	}

	private static SocketsHttpHandler CreateHandler(Proxy proxyDetails) {
		SocketsHttpHandler handler;
		if (proxyDetails.Bypass || proxyDetails.Host is null) {
			handler = new SocketsHttpHandler();
		} else {
			var proxy = new WebProxy(proxyDetails.Host);
			if (proxyDetails.Username is not null && proxyDetails.Password is not null) {
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