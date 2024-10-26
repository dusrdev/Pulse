namespace Pulse.Configuration;

/// <summary>
/// Execution mode
/// </summary>
public enum ExecutionMode {
	/// <summary>
	/// Execute requests sequentially
	/// </summary>
	Sequential,
	/// <summary>
	/// Execute requests in parallel
	/// </summary>
	Parallel
}