using System.Net;

namespace Pulse.Core;

public static class PulseHttpClientFactory {
	public static HttpClient Create(RequestDetails details) {
		if (details.BypassProxy) {
			return CreateDefault();
		}

		if (details.ProxyHost is null) {
			return CreateDefault();
		}
		return CreateWithProxy(details.ProxyHost, details.ProxyUsername, details.ProxyPassword);
	}

	private static HttpClient CreateDefault() {
		var handler = new SocketsHttpHandler();
		return new HttpClient(handler);
	}

	private static HttpClient CreateWithProxy(string host, string? username, string? password) {
		var proxy = new WebProxy(host);

		if (username is not null && password is not null) {
			proxy.Credentials = new NetworkCredential {
				UserName = username,
				Password = password
			};
		}

		var handler = new SocketsHttpHandler() {
			UseProxy = true,
			Proxy = proxy
		};
		return new HttpClient(handler);
	}
}