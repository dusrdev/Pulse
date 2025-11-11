using ConsoleAppFramework;

using Pulse.Configuration;

namespace Pulse.Core;

#pragma warning disable CA1031 // Do not catch general exception types

internal sealed class GlobalExceptionHandler(ConsoleAppFilter next) : ConsoleAppFilter(next) {
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken) {
        int startLine = GetCurrentLine();
        try {
            await Next.InvokeAsync(context, cancellationToken).ConfigureAwait(false);
        } catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
            ClearFrom(startLine);
            WriteLine(OutputPipe.Error, $"{Yellow}Cancellation requested and handled gracefully.");
            Environment.ExitCode = 1;
        } catch (Exception e) {
            ClearFrom(startLine);
            WriteLine(OutputPipe.Error, $"{Red}Unexpected exception! Please contact developer at: https://dusrdev.github.io");
            WriteLine(OutputPipe.Error, $"{Red}and provide the following details:");
            NewLine(OutputPipe.Error);
            Helper.PrintException(StrippedException.FromException(e));
            Environment.ExitCode = 1;
        }

        static void ClearFrom(int start) {
            int lines = GetCurrentLine() - start + 1;
            GoToLine(start);
            ClearNextLines(lines, OutputPipe.Error);
            GoToLine(start);
            ClearNextLines(lines, OutputPipe.Out);
            GoToLine(start);
        }
    }
}

#pragma warning restore CA1031 // Do not catch general exception types
