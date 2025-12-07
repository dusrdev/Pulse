using System.Text.Json;

namespace Pulse.Models;

internal readonly struct GetSampleModel : IOutputFormatter {
    public required string Path { get; init; }

    public void OutputAsJson() {
        JsonSerializer.ToConsoleOut(in this, ModelsJsonContext.Default.GetSampleModel);
    }

    public void OutputAsPlainText() {
        Console.WriteLineInterpolated($"Sample request generated at {Markup.Underline}{Yellow}{Path}{Markup.ResetUnderline}");
    }
}