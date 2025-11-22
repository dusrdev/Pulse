#:package PrettyConsole@5.0.0
#:package ConsoleAppFramework@5.7.11
#:package CliWrap@3.10.0

using CliWrap;
using CliWrap.Buffered;

using ConsoleAppFramework;

using PrettyConsole;

await ConsoleApp.RunAsync(args, Commands.Root);

static class Commands {
	public static async Task<int> Root(string directoryPath, CancellationToken ct) {
		string[][] commands = [
			["--help"], // show help
			["get-sample"], // get-sample json file
			["sample.json", "-u", "http://127.0.0.1:3000/"], // single request - default
			["sample.json", "-u", "http://127.0.0.1:3000/json/", "-n", "100", "--json"], // 100 concurrent + format
			["sample.json", "-u", "http://127.0.0.1:3000/html/", "-n", "100", "-c", "10", "--raw"], // no format
			["sample.json", "-u", "http://127.0.0.1:3000/html/", "-n", "10", "-f", "--raw"], // full equality
			["sample.json", "-u", "http://127.0.0.1:3000/html/", "-n", "100", "-v", "--raw"], // verbose mode
		];

		string appName = OperatingSystem.IsWindows()
						? "Pulse.exe"
						: "Pulse";

		string executable = Path.Join(directoryPath, appName);

		if (!File.Exists(executable)) {
			Console.WriteLineInterpolated($"{ConsoleColor.Red}Could not find the executable at {Markup.Underline}{executable}{Markup.ResetUnderline}.");
			return 1;
		}

		foreach (var command in commands) {
			var result = await Cli.Wrap(executable)
							.WithArguments(command)
							.WithWorkingDirectory(directoryPath)
							.ExecuteBufferedAsync(cancellationToken: ct);
			if (result.ExitCode != 0) {
				if (result.StandardOutput.Length > 0) {
					Console.WriteLineInterpolated($"{ConsoleColor.Green}Standard output:");
					Console.Write(result.StandardOutput.AsSpan(), OutputPipe.Out);
				}
				if (result.StandardError.Length > 0) {
					Console.WriteLineInterpolated($"{ConsoleColor.Green}Standard error:");
					Console.Write(result.StandardError.AsSpan(), OutputPipe.Out);
				}
				return 1;
			}
		}

		return 0;
	}
}
