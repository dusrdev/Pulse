using System.ComponentModel.DataAnnotations;

using ConsoleAppFramework;

using Pulse.Models;

namespace Pulse.Core;

#pragma warning disable CA1031 // Do not catch general exception types

internal sealed class GlobalExceptionHandler(ConsoleAppFilter next) : ConsoleAppFilter(next) {
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken) {
        if (context.GlobalOptions is not GlobalOptions options) {
            throw new InvalidCastException();
        }
        bool reportsProgress = !options.Quiet;

        int startLine = Console.GetCurrentLine();
        ConsoleState.Reset(startLine);
        try {
            await Next.InvokeAsync(context, cancellationToken).ConfigureAwait(false);
        } catch (Exception e) when (e is ValidationException or ArgumentParseFailedException) {
            throw;
        } catch (Exception e) when (e is TaskCanceledException or OperationCanceledException) {
            if (reportsProgress) {
                ClearFrom(startLine);
            }
            new StrippedException(nameof(OperationCanceledException), "").Output(options.Format);
            Environment.ExitCode = 1;
        } catch (Exception e) {
            if (reportsProgress) {
                ClearFrom(startLine);
            }
            StrippedException.FromException(e).Output(options.Format);
            Environment.ExitCode = 1;
        }

        static void ClearFrom(int start) {
            int last = Math.Max(Console.GetCurrentLine(), ConsoleState.LinesWritten);
            int lines = Math.Max(1, last - start + 1);
            Console.GoToLine(start);
            Console.ClearNextLines(lines);
            Console.GoToLine(start);
            Console.ClearNextLines(lines, OutputPipe.Out);
            Console.GoToLine(start);
        }
    }
}

#pragma warning restore CA1031 // Do not catch general exception types
