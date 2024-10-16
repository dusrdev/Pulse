using System.Text.Json;
using System.Text.Json.Serialization;

using Pulse.Core;

using Sharpify;

namespace Pulse.Configuration;

[JsonSourceGenerationOptions(AllowTrailingCommas = true,
							 DefaultIgnoreCondition = JsonIgnoreCondition.Never,
							 ReadCommentHandling = JsonCommentHandling.Skip,
							 UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
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

			using var file = File.OpenRead(path);
			var rd = JsonSerializer.Deserialize(file, Default.RequestDetails);
			if (rd is null) {
				return Result.Fail<RequestDetails>($"{path} - contained empty or invalid JSON.", new());
			}
			return Result.Ok(rd);
		} catch (Exception e) {
			var stripped = new StrippedException(e);
			var message = JsonSerializer.Serialize(stripped, Default.StrippedException);
			return Result.Fail<RequestDetails>(message, new());
		}
	}
}