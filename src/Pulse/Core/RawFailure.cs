using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// Represents a serializable way to display non successful response information when using <see cref="ParametersBase.ExportRaw"/>
/// </summary>
public readonly struct RawFailure {
    public RawFailure() {
		Headers = Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
		StatusCode = 0;
		Content = string.Empty;
    }

    /// <summary>
    /// Response status code
    /// </summary>
    public int StatusCode { get; init; }

	/// <summary>
	/// Response headers
	/// </summary>
	public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; }

	/// <summary>
	/// Response content (if any)
	/// </summary>
	public string Content { get; init; } = string.Empty;
}