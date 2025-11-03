using System.Text.Json;
using System.Text.Json.Serialization;

using Pulse.Core;

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
[JsonSerializable(typeof(Dictionary<string, IEnumerable<string>>))]
[JsonSerializable(typeof(RawFailure))]
[JsonSerializable(typeof(StrippedException))]
[JsonSerializable(typeof(ReleaseInfo))]
public partial class DefaultJsonContext : JsonSerializerContext {
    /// <summary>
    /// Deserializes the version from the release info JSON
    /// </summary>
    /// <param name="releaseInfoJson"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDeserializeVersion(ReadOnlySpan<char> releaseInfoJson, out Version? version) {
        version = null;
        var releaseInfo = JsonSerializer.Deserialize(releaseInfoJson, Default.ReleaseInfo);
        if (releaseInfo is null or { Version: null } || !Version.TryParse(releaseInfo.Version, out version)) {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Serializes a stripped exception to a string.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SerializeException(StrippedException e) => JsonSerializer.Serialize(e, Default.StrippedException);

    /// <summary>
    /// Serialize <see cref="RawFailure"/> to a string.
    /// </summary>
    /// <param name="failure"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Serialize(RawFailure failure) => JsonSerializer.Serialize(failure, Default.RawFailure);
}