using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

System.Console.CancelKeyPress += (_, e) => {
	e.Cancel = true;
	Services.Shared.Parameters.CancellationTokenSource.Cancel();
};

var firstLine = GetCurrentLine();

var cli = CliRunner.CreateBuilder()
					.AddCommand(SendCommand.Singleton)
					.UseConsoleAsOutputWriter()
					.WithMetadata(metadata => {
						metadata.Name = "Pulse";
						metadata.Author = "David Shnayder - dusrdev@gmail.com";
						metadata.Description =
						"""
						Hyper Fast General Purpose HTTP Request Tester

						[Disclaimer]
						By using this tool you agree to take full responsibility for the consequences of its use.
						""";
						metadata.Version = "1.0.0";
						metadata.License = "MIT";
					})
					.Build();

try {
	return await cli.RunAsync(args, false);
} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
	GoToLine(firstLine);
	ClearNextLines(4);
	ClearNextLinesError(4);
	WriteLine("Cancellation requested and handled gracefully." * Color.DarkYellow);
	return 1;
} catch (Exception e) {
	GoToLine(firstLine);
	ClearNextLines(4);
	ClearNextLinesError(4);
	WriteLineError("Unexpected error! Contact developer and provide the following output:" * Color.Red);
	NewLine();
	WriteLine(JsonContext.SerializeException(e));
	return 1;
}