using System.Text.Json.Serialization;

using Pulse.Core;

namespace Pulse.Configuration;

[JsonSourceGenerationOptions(AllowTrailingCommas = true,
							 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
							 IncludeFields = true,
							 NumberHandling = JsonNumberHandling.AllowReadingFromString,
							 WriteIndented = true,
							 UseStringEnumConverter = true)]
[JsonSerializable(typeof(ConcurrencyMode))]
[JsonSerializable(typeof(ParametersBase))]
[JsonSerializable(typeof(Parameters))]
[JsonSerializable(typeof(HttpRequestMessage))]
[JsonSerializable(typeof(RequestDetails))]
public partial class JsonContext : JsonSerializerContext {

}