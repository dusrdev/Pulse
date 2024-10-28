using System.Text;
using System.Text.Json;

namespace Pulse.Core;

/// <summary>
/// Request details
/// </summary>
public class RequestDetails {
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
	/// Ignore SSL errors
	/// </summary>
	public bool IgnoreSSL { get; set; }

	/// <summary>
	/// Host
	/// </summary>
	public string Host { get; set; } = "";

	/// <summary>
	/// Proxy authentication username
	/// </summary>
	public string Username { get; set; } = "";

	/// <summary>
	/// Proxy authentication password
	/// </summary>
	public string Password { get; set; } = "";
}

/// <summary>
/// Request configuration
/// </summary>
public class Request {
	public const string DefaultUrl = "https://ipinfo.io/geo";

	/// <summary>
	/// Request URL - defaults to https://ipinfo.io/geo
	/// </summary>
	public string Url { get; set; } = DefaultUrl;

	/// <summary>
	/// Request method - defaults to GET
	/// </summary>
	public HttpMethod Method { get; set; } = HttpMethod.Get;

	/// <summary>
	/// Request headers
	/// </summary>
	public Dictionary<string, JsonElement?> Headers { get; set; } = [];

	/// <summary>
	/// The request content
	/// </summary>
	public Content? Content { get; set; }

	/// <summary>
	/// Create an http request message from the configuration
	/// </summary>
	/// <returns><see cref="HttpRequestMessage"/></returns>
	public HttpRequestMessage CreateMessage() {
		var message = new HttpRequestMessage(Method, Url);

		foreach (var header in Headers) {
			if (header.Value is null) {
				continue;
			}
			message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
		}

		if (Content is not null && Content.Body.HasValue) {
			var media = Content.ContentType switch {
				"" => "application/json",
				_ => Content.ContentType
			};

			message.Content = new StringContent(Content.Body.ToString()!, Encoding.UTF8, media);
		}

		return message;
	}
}

/// <summary>
/// Request content
/// </summary>
public class Content {
	/// <summary>
	/// Declares the content type
	/// </summary>
	public string ContentType { get; set; } = "";

	/// <summary>
	/// Content
	/// </summary>
	public JsonElement? Body { get; set; }
}