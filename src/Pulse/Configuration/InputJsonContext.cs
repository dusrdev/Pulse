using System.Text.Json;
using System.Text.Json.Serialization;

using Pulse.Models;

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
internal partial class InputJsonContext : JsonSerializerContext {
    /// <summary>
    /// Try to get request details from file, do not attempt to use if returns false.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="details"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetRequestDetailsFromFile(string path, out RequestDetails details) {
        var json = File.ReadAllText(path);
        var rd = JsonSerializer.Deserialize(json, Default.RequestDetails);

        if (rd is null) {
            details = null!;
            return false;
        } else {
            details = rd;
            return true;
        }
    }
}