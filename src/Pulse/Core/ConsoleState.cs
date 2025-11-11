namespace Pulse.Core;

internal static class ConsoleState {
	private static readonly Lock Lock = new();
    public static int LinesWritten { get; set; }

    public static void Reset(int startLine) {
		lock (Lock) {
			LinesWritten = startLine;
		}
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
		lock (Lock) {
			if (LinesWritten >= candidate) return;
			LinesWritten = candidate;
		}
    }
}
