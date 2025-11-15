using ConsoleAppFramework;

using Pulse.Configuration;

namespace Pulse.Core;

#pragma warning disable CA1031 // Do not catch general exception types

internal sealed class GlobalExceptionHandler(ConsoleAppFilter next) : ConsoleAppFilter(next) {
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken) {
        int startLine = Console.GetCurrentLine();
        ConsoleState.Reset(startLine);
        try {
            await Next.InvokeAsync(context, cancellationToken).ConfigureAwait(false);
        } catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
            ClearFrom(startLine);
            Console.WriteLineInterpolated(OutputPipe.Error, $"{Yellow}Cancellation requested and handled gracefully.");
            Environment.ExitCode = 1;
        } catch (Exception e) {
            ClearFrom(startLine);
            Console.WriteLineInterpolated(OutputPipe.Error, $"{Red}Unexpected exception! Please contact developer at: {Markup.Underline}https://dusrdev.github.io{Markup.ResetUnderline}");
            Console.WriteLineInterpolated(OutputPipe.Error, $"{Red}and provide the following details:");
            Console.NewLine(OutputPipe.Error);
            Helper.PrintException(StrippedException.FromException(e));
            Environment.ExitCode = 1;
        }

        static void ClearFrom(int start) {
            int last = Math.Max(Console.GetCurrentLine(), ConsoleState.LinesWritten);
            int lines = Math.Max(1, last - start + 1);
            Console.GoToLine(start);
            Console.ClearNextLines(lines, OutputPipe.Error);
            Console.GoToLine(start);
            Console.ClearNextLines(lines, OutputPipe.Out);
            Console.GoToLine(start);
        }
    }
}

#pragma warning restore CA1031 // Do not catch general exception types
