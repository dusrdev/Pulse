namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public record ParametersBase {
	public static readonly ParametersBase Default = new();

	/// <summary>
	/// Default number of requests
	/// </summary>
	public const int DefaultNumberOfRequests = 1;

	/// <summary>
	/// Default batch size
	/// </summary>
	public const int DefaultBatchSize = 1;

	/// <summary>
	/// Default execution mode
	/// </summary>
	public const ExecutionMode DefaultExecutionMode = ExecutionMode.Parallel;

	/// <summary>
	/// Sets the number of requests (default = 100)
	/// </summary>
	public int Requests { get; set; } = DefaultNumberOfRequests;

	/// <summary>
	/// Sets the execution mode (default = <see cref="DefaultExecutionMode"/>)
	/// </summary>
	public ExecutionMode ExecutionMode { get; set; } = DefaultExecutionMode;

	/// <summary>
	/// Sets the batch size (default = <see cref="DefaultBatchSize"/>)
	/// </summary>
	public int BatchSize { get; set; } = DefaultBatchSize;

	/// <summary>
	/// Attempt to format response content as JSON
	/// </summary>
	public bool FormatJson { get; set; } = false;

	/// <summary>
	/// Indicating whether to export results
	/// </summary>
	public bool Export { get; set; } = true;

	/// <summary>
	/// Check full equality for response content
	/// </summary>
	public bool UseFullEquality { get; set; } = false;

	/// <summary>
	/// Display configuration and exit
	/// </summary>
	public bool NoOp { get; set; } = false;

	/// <summary>
	/// Display verbose output (adds more metrics)
	/// </summary>
	public bool Verbose { get; set; } = false;
}

/// <summary>
/// Execution parameters
/// </summary>
public record Parameters : ParametersBase {
	/// <summary>
	/// Application-wide cancellation token source
	/// </summary>
	public readonly CancellationTokenSource CancellationTokenSource = new();

	public Parameters(ParametersBase @base) : base(@base) { }
}