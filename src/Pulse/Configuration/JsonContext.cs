using System.Text.Json.Serialization;

using Pulse.Core;

namespace Pulse.Configuration;

[JsonSourceGenerationOptions(AllowTrailingCommas = true,
							 DefaultIgnoreCondition = JsonIgnoreCondition.Never,
							 ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
							 UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
							 PropertyNameCaseInsensitive = true,
							 UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
							 IncludeFields = true,
							 NumberHandling = JsonNumberHandling.AllowReadingFromString,
							 WriteIndented = true,
							 UseStringEnumConverter = true)]
[JsonSerializable(typeof(RequestDetails))]
public partial class JsonContext : JsonSerializerContext {

}