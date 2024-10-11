namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public class ParametersBase {
	public static readonly ParametersBase Default = new();

	/// <summary>
	/// Sets the number of requests (default = 100)
	/// </summary>
	public int Requests { get; set; } = 100;

	/// <summary>
	/// Concurrency Mode
	/// </summary>
	public bool UseConcurrency { get; set; } = true;

	/// <summary>
	/// Indicating whether to use resilience (apply jitting)
	/// </summary>
	public bool UseResilience { get; set; } = false;

	/// <summary>
	/// Indicating whether to bypass exports
	/// </summary>
	public bool NoExport { get; set; } = false;

	/// <summary>
	/// Check full equality for response content
	/// </summary>
	public bool UseFullEquality { get; set; } = false;

	/// <summary>
	/// Use verbose output
	/// </summary>
	public bool NoOp { get; set; } = false;
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