using System.Text.Json;

namespace Pulse.Models;

internal readonly struct CheckForUpdatesModel : IOutputFormatter {
	public required Version CurrentVersion { get; init; }
	public required Version RemoteVersion { get; init; }
	public required bool UpdateRequired { get; init; }
	public string RemoteRepository { get; init; } = RemoteRepositoryUrl;
	private const string RemoteRepositoryUrl = "https://github.com/dusrdev/Pulse/releases/latest";

    public CheckForUpdatesModel() {
    }

    public void OutputAsJson() {
		JsonSerializer.ToConsoleOut(in this, ModelsJsonContext.Default.CheckForUpdatesModel);
	}

	public void OutputAsPlainText() {
		if (UpdateRequired) {
			Console.WriteLineInterpolated($"{Yellow}A new version of Pulse is available!");
			Console.WriteLineInterpolated($"Your version: {Markup.Underline}{Yellow}{CurrentVersion}{Markup.ResetUnderline}");
			Console.WriteLineInterpolated($"Latest version: {Markup.Underline}{Green}{RemoteVersion}{Markup.ResetUnderline}");
			Console.NewLine();
			Console.WriteLineInterpolated($"Download from {Markup.Underline}{RemoteRepository}{Markup.ResetUnderline}");
		} else {
			Console.WriteLineInterpolated($"{Green}You are using the latest version of Pulse.");
		}
	}
}