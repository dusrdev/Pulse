using System.Collections.Concurrent;

namespace Pulse.Core;

public record PulseResult {
	public required ConcurrentStack<RequestResult> Results { get; init; }
	
}