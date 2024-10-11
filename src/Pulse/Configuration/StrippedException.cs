using System.Text.Json.Serialization;

namespace Pulse.Configuration;

public sealed record StrippedException {
	public readonly string Type;
	public readonly string Message;
	public readonly string StackTrace;

	public StrippedException(Exception? exception) {
		if (exception is null) {
			Type = "";
			Message = "";
			StackTrace = "";
			return;
		}

		Type = exception.GetType().Name;
		Message = exception.Message;
		StackTrace = exception.StackTrace ?? "";
	}

	[JsonConstructor]
	public StrippedException() {
		Type = "";
		Message = "";
		StackTrace = "";
	}
}