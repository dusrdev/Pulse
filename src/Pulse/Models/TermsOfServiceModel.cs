using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Models;

internal readonly ref partial struct TermsOfServiceModel : IOutputFormatter<TermsOfServiceModel> {
	private static ImmutableArray<string> Lines => ImmutableArray.Create([
		"By using this tool you agree to take full responsibility for the consequences of its use.",
		"Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's responsibility to obey all applicable local, state and federal laws.",
		"The developers assume no liability and are not responsible for any misuse or damage caused by this program."
	]);

	public void OutputAsJson() {
		using var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
		JsonSerializer.Serialize(writer, Lines, JsonContext.Default.ImmutableArrayString);
		writer.Flush();
	}

	public void OutputAsPlainText() {
		foreach (var line in Lines) {
			Console.WriteLineInterpolated($"{line}");
		}
	}

	[JsonSerializable(typeof(ImmutableArray<string>))]
	private partial class JsonContext : JsonSerializerContext;
}