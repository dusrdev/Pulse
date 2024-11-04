using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

using CancellationTokenSource globalCTS = new();

System.Console.CancelKeyPress += (_, e) => {
	e.Cancel = true;
	globalCTS.Cancel();
};

var firstLine = GetCurrentLine();

const string version = "1.0.0";

var cli = CliRunner.CreateBuilder()
					.AddCommand(new SendCommand(globalCTS))
					.UseConsoleAsOutputWriter()
					.WithMetadata(metadata => metadata.Version = version)
					.WithCustomHeader(
						$"""
						Pulse - A hyper fast general purpose HTTP request tester

						Made by: David Shnayder - dusrdev@gmail.com
						Version: {version}
						License: MIT
						"""
					)
					.SetHelpTextSource(HelpTextSource.CustomHeader)
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