using System.Collections.Concurrent;

namespace Pulse.Core;

public record PulseResult {
	public required ConcurrentStack<RequestResult> Results { get; init; }

	public required int TotalCount { get; init; }

	public required TimeSpan TotalDuration { get; init; }

	public required double SuccessRate { get; init; }
}