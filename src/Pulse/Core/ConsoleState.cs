namespace Pulse.Core;

internal static class ConsoleState {
	public static int LinesWritten {
		get {
			return field;
		}
		set {
			Interlocked.Exchange(ref field, value);
		}
	}
}