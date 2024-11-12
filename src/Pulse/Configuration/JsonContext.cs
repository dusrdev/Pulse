using System.Text.Json;
using System.Text.Json.Serialization;

using Pulse.Core;

using Sharpify;

namespace Pulse.Configuration;

[JsonSourceGenerationOptions(AllowTrailingCommas = true,
							 DefaultIgnoreCondition = JsonIgnoreCondition.Never,
							 ReadCommentHandling = JsonCommentHandling.Skip,
							 UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
							 PropertyNameCaseInsensitive = true,
							 UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
							 IncludeFields = true,
							 NumberHandling = JsonNumberHandling.AllowReadingFromString,
							 WriteIndented = true,
							 UseStringEnumConverter = true)]
[JsonSerializable(typeof(RequestDetails))]
[JsonSerializable(typeof(StrippedException))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(ReleaseInfo))]
public partial class JsonContext : JsonSerializerContext {
	/// <summary>
	/// Try to get request details from file
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static Result<RequestDetails> TryGetRequestDetailsFromFile(string path) {
		try {
			if (!File.Exists(path)) {
				return Result.Fail($"{path} - does not exist.", new RequestDetails());
			}

			var json = File.ReadAllText(path);
			var rd = JsonSerializer.Deserialize(json, Default.RequestDetails);
			if (rd is null) {
				return Result.Fail($"{path} - contained empty or invalid JSON.", new RequestDetails());
			}
			return Result.Ok(rd);
		} catch (Exception e) {
			var stripped = Configuration.StrippedException.FromException(e);
			var message = JsonSerializer.Serialize(stripped, Default.StrippedException);
			return Result.Fail(message, new RequestDetails());
		}
	}

	/// <summary>
	/// Deserializes the version from the release info JSON
	/// </summary>
	/// <param name="releaseInfoJson"></param>
	/// <returns></returns>
	public static Result DeserializeVersion(ReadOnlySpan<char> releaseInfoJson) {
		try {
			var releaseInfo = JsonSerializer.Deserialize(releaseInfoJson, Default.ReleaseInfo);
			if (releaseInfo is null or { Body: null }) {
				return Result.Fail("Invalid JSON");
			}
			return Result.Ok(releaseInfo.Body);
		} catch {
			return Result.Fail("Invalid JSON");
		}
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