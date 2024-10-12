using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

//TODO: Ensure all correct dependencies are passed in constructors.

System.Console.CancelKeyPress += (_, _) => Services.Instance.Parameters.CancellationTokenSource.Cancel();

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
						metadata.Version = "1.0.0-rc1";
						metadata.License = "MIT";
					})
					.Build();

try {
	return await cli.RunAsync(args, false);
} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
	ClearNextLines(4);
	WriteLine("Canceled requested and handled gracefully." * Color.DarkYellow);
	return 1;
} catch (Exception e) {
	WriteLineError("Unexpected error! Contact developer and provide the following output:" * Color.Red);
	NewLine();
	WriteLine(["Message: ", e.Message * Color.Yellow]);
	if (e.StackTrace is not null) {
		WriteLine(["Stack Trace: " * Color.Yellow, e.StackTrace]);
	}
	return 1;
}