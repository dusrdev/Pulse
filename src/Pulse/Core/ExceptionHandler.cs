using ConsoleAppFramework;

using Pulse.Configuration;

namespace Pulse.Core;

internal sealed class GlobalExceptionHandler(ConsoleAppFilter next) : ConsoleAppFilter(next) {
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken) {
        int startLine = GetCurrentLine();
        try {
            await Next.InvokeAsync(context, cancellationToken);
        } catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
            ClearFrom(startLine);
            WriteLine(OutputPipe.Error, $"{Yellow}Cancellation requested and handled gracefully.");
            Environment.ExitCode = 1;
        } catch (Exception e) {
            ClearFrom(startLine);
            WriteLine(OutputPipe.Error, $"{Red}Unexpected exception! Please contact developer at dusrdev@gmail.com and provide the following:");
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