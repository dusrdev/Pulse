using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Pulse.Models;

[JsonSerializable(typeof(SummaryModel))]
[JsonSerializable(typeof(MinMeanMax))]
[JsonSerializable(typeof(Target))]
[JsonSerializable(typeof(ImmutableArray<string>))]
internal partial class ModelsJsonContext : JsonSerializerContext;

internal static class JsonSerializerExtensions {
	extension(JsonSerializer) {
		internal static void ToConsoleOut<T>(in T value, JsonTypeInfo<T> jsonTypeInfo) {
			using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
			JsonSerializer.Serialize(writer, value, jsonTypeInfo);
			writer.Flush();
		}
	}
}