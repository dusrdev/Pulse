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
[JsonSerializable(typeof(IEnumerable<KeyValuePair<string, IEnumerable<string>>>))]
[JsonSerializable(typeof(RawFailure))]
[JsonSerializable(typeof(StrippedException))]
[JsonSerializable(typeof(ReleaseInfo))]
public partial class DefaultJsonContext : JsonSerializerContext {
	/// <summary>
	/// Deserializes the version from the release info JSON
	/// </summary>
	/// <param name="releaseInfoJson"></param>
	/// <returns></returns>
	public static Result<Version> DeserializeVersion(ReadOnlySpan<char> releaseInfoJson) {
		var releaseInfo = JsonSerializer.Deserialize(releaseInfoJson, Default.ReleaseInfo);
		if (releaseInfo is null or { Version: null } || !Version.TryParse(releaseInfo.Version, out Version? remoteVersion)) {
			return Result.Fail("Failed to retrieve version for remote");
		}
		return Result.Ok(remoteVersion);
	}

	/// <summary>
	/// Serialize <see cref="RawFailure"/> to a string.
	/// </summary>
	/// <param name="failure"></param>
	public static string Serialize(RawFailure failure) => JsonSerializer.Serialize(failure, Default.RawFailure);

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