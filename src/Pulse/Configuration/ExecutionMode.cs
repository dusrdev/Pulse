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
	/// Execute requests such that only 1 requests per core is allowed
	/// </summary>
	Bounded,
	/// <summary>
	/// Execute requests such that no limit is imposed - requests are executed as fast as possible
	/// </summary>
	Unbounded
}