using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

//TODO: Ensure all correct dependencies are passed in constructors.

System.Console.CancelKeyPress += (_, _) => {
	Services.Instance.Parameters.CancellationTokenSource.Cancel();
	ClearNextLines(4);
	WriteLine("Canceled gracefully." * Color.DarkYellow);
};

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

return await cli.RunAsync(args);