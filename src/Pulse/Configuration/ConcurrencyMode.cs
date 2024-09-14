namespace Pulse.Configuration;

/// <summary>
/// The concurrency mode for the application
/// </summary>
public enum ConcurrencyMode {
	/// <summary>
	/// Run as much requests in parallel as hardware can handle
	/// </summary>
	Maximum,

	/// <summary>
	/// Limits the amount of parallel requests, uses specified amount
	/// </summary>
	Limited,

	/// <summary>
	/// Equivalent to <see cref="Limited"/> with value = 1, so sequential
	/// </summary>
	Disabled
}