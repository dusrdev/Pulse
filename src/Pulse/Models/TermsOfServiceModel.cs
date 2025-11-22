using System.Collections.Immutable;
using System.Text.Json;

namespace Pulse.Models;

internal readonly struct TermsOfServiceModel : IOutputFormatter {
	private static ImmutableArray<string> Lines => ImmutableArray.Create([
		"By using this tool you agree to take full responsibility for the consequences of its use.",
		"Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's responsibility to obey all applicable local, state and federal laws.",
		"The developers assume no liability and are not responsible for any misuse or damage caused by this program."
	]);

	public void OutputAsJson() {
		JsonSerializer.ToConsoleOut(Lines, ModelsJsonContext.Default.ImmutableArrayString);
	}

	public void OutputAsPlainText() {
		foreach (var line in Lines) {
			Console.WriteLineInterpolated($"{line}");
		}
	}
}