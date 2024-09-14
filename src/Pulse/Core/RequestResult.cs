using System.Net;

namespace Pulse.Core;

public record RequestResult {
	public required HttpStatusCode? StatusCode { get; init; }
	public required string? Content { get; init; }

	public required TimeSpan Duration { get; init; }
	public required Exception? Exception { get; init; }
}