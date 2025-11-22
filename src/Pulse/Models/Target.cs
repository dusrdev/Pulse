namespace Pulse.Models;

internal readonly struct Target {
	public readonly string HttpMethod { get; init; }
	public readonly string Url { get; init; }
}