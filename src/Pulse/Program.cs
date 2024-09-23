using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

//TODO: Ensure all correct dependencies are passed in constructors.
//TODO: Recheck options in SendCommand

System.Console.CancelKeyPress += (_, _) => Services.Instance.Parameters.CancellationTokenSource.Cancel();

var cli = CliRunner.CreateBuilder()
					.AddCommand(SendCommand.Singleton)
					.AddCommand(CustomCommand.Singleton)
					.UseConsoleAsOutputWriter()
					.WithMetadata(metadata => {
						metadata.Name = "Pulse";
						metadata.Author = "David Shnayder - dusrdev@gmail.com";
						metadata.Description = "Lightning Fast - Hyper Optimized General Purpose HTTP Request Tester";
						metadata.Version = "Alpha 0.9";
						metadata.License = "MIT";
					})
					.Build();

try {
	return await cli.RunAsync(args);
} catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
	ClearNextLines(4);
	WriteLine("Canceled requested and handled gracefully." * Color.DarkYellow);
	return 1;
} catch (Exception e) {
	WriteLineError("Unexpected error! Contact developer and provide the following output:" * Color.Red);
	NewLine();
	WriteLine("Type: ", e.GetType().Name * Color.Yellow);
	WriteLine("Message: ", e.Message * Color.Yellow);
	if (e.StackTrace is not null) {
		WriteLine("Stack Trace: " * Color.Yellow, e.StackTrace);
	}
	return 1;
}