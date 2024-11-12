namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public record ParametersBase {
	/// <summary>
	/// Default number of requests
	/// </summary>
	public const int DefaultNumberOfRequests = 1;

	/// <summary>
	/// Default maximum active connections
	/// </summary>
	public const int DefaultMaxConnections = 1;

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
	public ExecutionMode ExecutionMode { get; init; } = DefaultExecutionMode;

	/// <summary>
	/// Sets the maximum connections (default = <see cref="DefaultMaxConnections"/>)
	/// </summary>
	public int MaxConnections { get; init; } = DefaultMaxConnections;

	/// <summary>
	/// Indicating whether the max connections parameter was modified
	/// </summary>
	public bool MaxConnectionsModified { get; init; }

	/// <summary>
	/// Attempt to format response content as JSON
	/// </summary>
	public bool FormatJson { get; init; }

	/// <summary>
	/// Indicating whether to export results
	/// </summary>
	public bool Export { get; init; } = true;

	/// <summary>
	/// Check full equality for response content
	/// </summary>
	public bool UseFullEquality { get; init; }

	/// <summary>
	/// Display configuration and exit
	/// </summary>
	public bool NoOp { get; init; }

	/// <summary>
	/// Display verbose output (adds more metrics)
	/// </summary>
	public bool Verbose { get; init; }

	/// <summary>
	/// Output folder
	/// </summary>
	public string OutputFolder { get; init; } = "results";
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