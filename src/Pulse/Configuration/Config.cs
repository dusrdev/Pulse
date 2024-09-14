namespace Pulse.Configuration;

/// <summary>
/// Main app configuration
/// </summary>
public record Config {
	public static readonly Config Default = new();

	/// <summary>
	/// Sets the concurrency mode
	/// </summary>
	public ConcurrencyMode ConcurrencyMode { get; set; } = ConcurrencyMode.Maximum;

	/// <summary>
	/// Sets the number of requests (default = 100)
	/// </summary>
	public int Requests { get; set; } = 100;

	/// <summary>
	/// The amount of concurrent requests when <see cref="ConcurrencyMode"/> is set to <see cref="ConcurrencyMode.Limited"/>
	/// </summary>
	public int ConcurrentRequests { get; set; } = 1;

	/// <summary>
	/// Indicating whether to use resilience (apply jitting)
	/// </summary>
	public bool UseResilience { get; set; }

	/// <summary>
	/// Indicating whether to bypass exports
	/// </summary>
	public bool BypassExport { get; set; }

}