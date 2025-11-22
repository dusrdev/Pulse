namespace Pulse.Models;

internal readonly struct Target {
    public string HttpMethod { get; init; }
    public string Url { get; init; }
}