namespace Pulse.Configuration;

/// <summary>
/// Execution parameters
/// </summary>
public record ParametersBase {
    /// <summary>
    /// Sets the number of requests (default = 100)
    /// </summary>
    public int Requests { get; set; } = 1;

    /// <summary>
    /// Sets the timeout in milliseconds
    /// </summary>
    public int TimeoutInMs { get; set; } = -1;

    /// <summary>
    /// The delay between requests in milliseconds
    /// </summary>
    public int DelayInMs { get; set; } = 0;

    /// <summary>
    /// Sets the maximum connections
    /// </summary>
    public int Connections { get; init; } = 1;

    /// <summary>
    /// Attempt to format response content as JSON
    /// </summary>
    public bool FormatJson { get; init; }

    /// <summary>
    /// Indicating whether to export raw results (without wrapping in custom html)
    /// </summary>
    public bool ExportRaw { get; init; }

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