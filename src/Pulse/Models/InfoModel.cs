using System.Text.Json;

namespace Pulse.Models;

internal readonly struct InfoModel : IOutputFormatter {
	public required string Author { get; init; }
	public required string Version { get; init; }
	public required string License { get; init; }
	public required string Repository { get; init; }

    public void OutputAsJson() {
		JsonSerializer.ToConsoleOut(in this, ModelsJsonContext.Default.InfoModel);
    }

    public void OutputAsPlainText() {
		Console.WriteLineInterpolated($"Written by {Green}{Markup.Underline}{Author}{Markup.ResetUnderline}{ConsoleColor.DefaultForeground}.");
		Console.WriteLineInterpolated($"Version: {Version}");
		Console.WriteLineInterpolated($"License: {License}");
		Console.WriteLineInterpolated($"Repository: {Markup.Underline}{Repository}{Markup.ResetUnderline}");
    }
}