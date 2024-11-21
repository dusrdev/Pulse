using System.Text.Json.Serialization;

namespace Pulse.Core;

/// <summary>
/// Release information
/// </summary>
public sealed class ReleaseInfo {
	/// <summary>
	/// Version is taken from the release tag
	/// </summary>
	[JsonPropertyName("tag_name")]
	public string? Version { get; set; }
}