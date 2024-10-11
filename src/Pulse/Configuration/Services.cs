using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Configuration;

public sealed class Services {
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static readonly Services Instance = new();

	// public readonly JsonSerializerOptions JsonOptions;

	public readonly Parameters Parameters;

	private Services() {
		// JsonOptions = new() {
		// 	WriteIndented = true,
		// 	IncludeFields = true,
		// 	NumberHandling = JsonNumberHandling.AllowReadingFromString,
		// 	AllowTrailingCommas = true,
		// 	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		// };

		// JsonOptions.Converters.Add(new ExceptionConverter());

		Parameters = new();
	}
}
