namespace Pulse.Command;

public class RequestDetails {
	public static readonly RequestDetails Default = new();

	public bool BypassProxy { get; set; }
	public string? ProxyHost { get; set; }
	public string? ProxyUsername { get; set; }
	public string? ProxyPassword { get; set; }

	public HttpRequestMessage? RequestMessage { get; set; } = new(HttpMethod.Get, "https://ipinfo.io/geo");
}