using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Configuration;

public sealed class Services {
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static readonly Services Instance = new();

	/// <summary>
	/// The global parameters for the execution scope
	/// </summary>
	public readonly Parameters Parameters;

	private Services() {
		Parameters = new();
	}
}
