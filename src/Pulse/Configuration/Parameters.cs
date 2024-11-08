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
	/// Default timeout in milliseconds (infinity)
	/// </summary>
	public const int DefaultTimeoutInMs = -1;

	/// <summary>
	/// Default execution mode
	/// </summary>
	public const ExecutionMode DefaultExecutionMode = ExecutionMode.Parallel;

	/// <summary>
	/// Sets the number of requests (default = 100)
	/// </summary>
	public int Requests { get; set; } = DefaultNumberOfRequests;

	/// <summary>
	/// Sets the timeout in milliseconds
	/// </summary>
	public int TimeoutInMs { get; set; } = DefaultTimeoutInMs;

	/// <summary>
	/// Sets the execution mode (default = <see cref="DefaultExecutionMode"/>)
	/// </summary>
	public ExecutionMode ExecutionMode { get; set; } = DefaultExecutionMode;

	/// <summary>
	/// Sets the batch size (default = <see cref="DefaultBatchSize"/>)
	/// </summary>
	public int BatchSize { get; set; } = DefaultBatchSize;

	/// <summary>
	/// Indicating whether the batch size was modified
	/// </summary>
	public bool BatchSizeModified { get; set; }

	/// <summary>
	/// Attempt to format response content as JSON
	/// </summary>
	public bool FormatJson { get; set; }

	/// <summary>
	/// Indicating whether to export results
	/// </summary>
	public bool Export { get; set; } = true;

	/// <summary>
	/// Check full equality for response content
	/// </summary>
	public bool UseFullEquality { get; set; }

	/// <summary>
	/// Display configuration and exit
	/// </summary>
	public bool NoOp { get; set; }

	/// <summary>
	/// Display verbose output (adds more metrics)
	/// </summary>
	public bool Verbose { get; set; }

	/// <summary>
	/// Output folder
	/// </summary>
	public string OutputFolder { get; set; } = "results";
}

/// <summary>
/// Execution parameters
/// </summary>
public sealed record Parameters : ParametersBase {
	/// <summary>
	/// Application-wide cancellation token
	/// </summary>
	public readonly CancellationToken CancellationToken;

	public Parameters(ParametersBase @base, CancellationToken cancellationToken) : base(@base) {
		CancellationToken = cancellationToken;
	}
}