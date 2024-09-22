using System.Net;

namespace Pulse.Core;

public static class PulseHttpClientFactory {
	public static HttpClient Create(RequestDetails details) {
		var proxy = details.Proxy;

		if (proxy.Bypass) {
			return CreateDefault();
		}

		if (proxy.Host is null) {
			return CreateDefault();
		}

		return CreateWithProxy(proxy);
	}

	private static HttpClient CreateDefault() {
		var handler = new SocketsHttpHandler();
		return new HttpClient(handler);
	}

	private static HttpClient CreateWithProxy(Proxy proxyDetails) {
		var proxy = new WebProxy(proxyDetails.Host);

		if (proxyDetails.Username is not null && proxyDetails.Password is not null) {
			proxy.Credentials = new NetworkCredential {
				UserName = proxyDetails.Username,
				Password = proxyDetails.Password
			};
		}

		var handler = new SocketsHttpHandler() {
			UseProxy = true,
			Proxy = proxy
		};
		return new HttpClient(handler);
	}
}