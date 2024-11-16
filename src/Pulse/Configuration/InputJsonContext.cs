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
[JsonSerializable(typeof(JsonElement))]
public partial class InputJsonContext : JsonSerializerContext {
	/// <summary>
	/// Try to get request details from file
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static Result<RequestDetails> TryGetRequestDetailsFromFile(string path) {
		if (!File.Exists(path)) {
			return Result.Fail($"{path} - does not exist.", new RequestDetails());
		}

		var json = File.ReadAllText(path);
		var rd = JsonSerializer.Deserialize(json, Default.RequestDetails);
		if (rd is null) {
			return Result.Fail($"{path} - contained empty or invalid JSON.", new RequestDetails());
		}
		return Result.Ok(rd);
	}
}