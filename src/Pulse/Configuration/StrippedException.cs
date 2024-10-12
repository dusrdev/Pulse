using System.Text.Json.Serialization;

namespace Pulse.Configuration;

public sealed record StrippedException {
	public static readonly StrippedException Default = new();

	public readonly string Type;
	public readonly string Message;
	public readonly string StackTrace;
	public readonly bool IsDefault;

	public StrippedException(Exception exception) {
		Type = exception.GetType().Name;
		Message = exception.Message;
		StackTrace = exception.StackTrace ?? "";
	}

	[JsonConstructor]
	public StrippedException() {
		Type = "";
		Message = "";
		StackTrace = "";
		IsDefault = true;
	}
}