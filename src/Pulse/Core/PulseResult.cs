using System.Collections.Concurrent;

namespace Pulse.Core;

/// <summary>
/// Result of pulse (complete test)
/// </summary>
public record PulseResult {
	/// <summary>
	/// Results of the individual requests
	/// </summary>
	public required ConcurrentStack<Response> Results { get; init; }

	/// <summary>
	/// Total duration of the pulse
	/// </summary>
	public required TimeSpan TotalDuration { get; init; }

	/// <summary>
	/// Success rate (percentage of 2xx responses)
	/// </summary>
	public required double SuccessRate { get; init; }
}