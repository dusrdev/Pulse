namespace Pulse.Core;

/// <summary>
/// Request details
/// </summary>
public class RequestDetails {
	/// <summary>
	/// Default request details
	/// </summary>
	public static readonly RequestDetails Default = new();

	/// <summary>
	/// Proxy configuration
	/// </summary>
	public Proxy Proxy { get; set; } = new();

	/// <summary>
	/// Request configuration
	/// </summary>
	public Request Request { get; set; } = new();
}

/// <summary>
/// Proxy configuration
/// </summary>
public class Proxy {
	/// <summary>
	/// Don't use proxy
	/// </summary>
	public bool Bypass { get; set; } = true;

	/// <summary>
	/// Host
	/// </summary>
	public string? Host { get; set; }

	/// <summary>
	/// Proxy authentication username
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	/// Proxy authentication password
	/// </summary>
	public string? Password { get; set; }
}

/// <summary>
/// Request configuration
/// </summary>
public class Request {
	/// <summary>
	/// Request URL - defaults to https://ipinfo.io/geo
	/// </summary>
	public string Url { get; set; } = "https://ipinfo.io/geo";

	/// <summary>
	/// Request method - defaults to GET
	/// </summary>
	public HttpMethod Method { get; set; } = HttpMethod.Get;

	/// <summary>
	/// Request headers
	/// </summary>
	public Dictionary<string, string?> Headers { get; set; } = [];

	/// <summary>
	/// Request body
	/// </summary>
	public string? Body { get; set; }

	/// <summary>
	/// Create an http request message from the configuration
	/// </summary>
	/// <returns><see cref="HttpRequestMessage"/></returns>
	public HttpRequestMessage CreateMessage() {
		var message = new HttpRequestMessage(Method, Url);

		foreach (var header in Headers) {
			message.Headers.Add(header.Key, header.Value);
		}

		if (Body is not null) {
			message.Content = new StringContent(Body);
		}

		return message;
	}
}