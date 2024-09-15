using Pulse.Core;

using Sharpify.CommandLineInterface;

//TODO: Add graceful cancellation

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