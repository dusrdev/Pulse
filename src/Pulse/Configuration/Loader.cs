using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Pulse.Configuration;

public static class Loader {
	public static T Load<T>(string path, JsonTypeInfo<T> typeInfo, T @default) {
		if (!File.Exists(path)) {
			return CreateAndReturn(path, typeInfo, @default);
		} else {
			try {
				using var file = File.OpenRead(path);
				T? item = JsonSerializer.Deserialize(file, typeInfo);
                return item is not null ? item : throw new NullReferenceException($"Deserialization of {nameof(T)} failed!");
            } catch {
				return CreateAndReturn(path, typeInfo, @default);
			}
		}

		static T CreateAndReturn(string path, JsonTypeInfo<T> typeInfo, T @default) {
			using var file = File.Open(path, FileMode.OpenOrCreate);
			T item = @default;
			JsonSerializer.Serialize(file, item, typeInfo);
			return item;
		}
	}
}