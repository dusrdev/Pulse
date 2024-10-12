namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public class ParametersBase {
	public static readonly ParametersBase Default = new();

	/// <summary>
	/// Default number of requests
	/// </summary>
	public const int DefaultNumberOfRequests = 1;

	/// <summary>
	/// Default execution mode
	/// </summary>
	public const ExecutionMode DefaultExecutionMode = ExecutionMode.Unbounded;

	/// <summary>
	/// Sets the number of requests (default = 100)
	/// </summary>
	public int Requests { get; set; } = DefaultNumberOfRequests;

	/// <summary>
	/// Sets the execution mode (default = <see cref="DefaultExecutionMode"/>)
	/// </summary>
	public ExecutionMode ExecutionMode { get; set; } = DefaultExecutionMode;

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