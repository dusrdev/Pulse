namespace Pulse.Core;

internal static class ConsoleState {
    public static int LinesWritten {
        get => field;
        set => Interlocked.Exchange(ref field, value);
    }

    public static void Reset(int startLine) {
		LinesWritten = startLine;
    }

    public static void ReportLinesFromCurrent(int lineCount) {
        if (lineCount <= 0) {
            return;
        }

        int current = GetCurrentLine();
        int lastLine = current + lineCount - 1;
        UpdateMax(lastLine);
    }

    private static void UpdateMax(int candidate) {
		if (LinesWritten >= candidate) return;
		LinesWritten = candidate;
    }
}
