namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public class ParametersBase {
	public static readonly ParametersBase Default = new();

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
	public bool NoExport { get; set; }

	/// <summary>
	/// Check full equality for response content
	/// </summary>
	public bool UseFullEquality { get; set; }
}

/// <summary>
/// Execution parameters
/// </summary>
public sealed class Parameters : ParametersBase {
	/// <summary>
	/// Application-wide cancellation token source
	/// </summary>
	public readonly CancellationTokenSource CancellationTokenSource = new();
}