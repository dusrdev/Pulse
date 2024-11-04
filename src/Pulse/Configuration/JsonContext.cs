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
public partial class JsonContext : JsonSerializerContext {
	/// <summary>
	/// Try to get request details from file
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static Result<RequestDetails> TryGetRequestDetailsFromFile(string path) {
		try {
			if (!File.Exists(path)) {
				return Result.Fail<RequestDetails>($"{path} - does not exist.", new());
			}

			var json = File.ReadAllText(path);
			var rd = JsonSerializer.Deserialize(json, Default.RequestDetails);
			if (rd is null) {
				return Result.Fail<RequestDetails>($"{path} - contained empty or invalid JSON.", new());
			}
			return Result.Ok(rd);
		} catch (Exception e) {
			var stripped = Configuration.StrippedException.FromException(e);
			var message = JsonSerializer.Serialize(stripped, Default.StrippedException);
			return Result.Fail<RequestDetails>(message, new());
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