using System.Collections.Concurrent;

namespace Pulse.Command;

public record PulseResult {
	public required ConcurrentStack<RequestResult> Results { get; init; }
	
}