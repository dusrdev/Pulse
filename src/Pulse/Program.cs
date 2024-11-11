using Pulse.Configuration;
using Pulse.Core;

using Sharpify.CommandLineInterface;

using static PrettyConsole.Console;
using PrettyConsole;

internal class Program {
	internal const string VERSION = "1.0.4.0";

    private static async Task<int> Main(string[] args) {
        using CancellationTokenSource globalCTS = new();

        System.Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            globalCTS.Cancel();
        };

        var firstLine = GetCurrentLine();

        var cli = CliRunner.CreateBuilder()
                            .AddCommand(new SendCommand(globalCTS.Token))
                            .UseConsoleAsOutputWriter()
                            .WithMetadata(metadata => metadata.Version = VERSION)
                            .WithCustomHeader(
                                $"""
						Pulse - A hyper fast general purpose HTTP request tester

						Repository: https://github.com/dusrdev/Pulse
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
    }
}