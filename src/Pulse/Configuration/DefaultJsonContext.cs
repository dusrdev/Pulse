using System.Text.Json;
using System.Text.Json.Serialization;

using Pulse.Core;

using Sharpify;

namespace Pulse.Configuration;

[JsonSourceGenerationOptions(AllowTrailingCommas = true,
							 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull,
							 UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
							 PropertyNameCaseInsensitive = true,
							 UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
							 IncludeFields = true,
							 NumberHandling = JsonNumberHandling.AllowReadingFromString,
							 WriteIndented = true,
							 UseStringEnumConverter = true)]
[JsonSerializable(typeof(StrippedException))]
[JsonSerializable(typeof(ReleaseInfo))]
public partial class DefaultJsonContext : JsonSerializerContext {
	/// <summary>
	/// Deserializes the version from the release info JSON
	/// </summary>
	/// <param name="releaseInfoJson"></param>
	/// <returns></returns>
	public static Result DeserializeVersion(ReadOnlySpan<char> releaseInfoJson) {
		var releaseInfo = JsonSerializer.Deserialize(releaseInfoJson, Default.ReleaseInfo);
		if (releaseInfo is null or { Body: null }) {
			return Result.Fail("Invalid JSON");
		}
		return Result.Ok(releaseInfo.Body);
	}

	/// <summary>
	/// Serializes an exception to a string.
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public static string SerializeException(Exception e) => SerializeException(Configuration.StrippedException.FromException(e));

	/// <summary>
	/// Serializes a stripped exception to a string.
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public static string SerializeException(StrippedException e) => JsonSerializer.Serialize(e, Default.StrippedException);
}