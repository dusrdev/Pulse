using System.Text.Json.Serialization;

namespace Pulse.Configuration;

/// <summary>
/// An exception only containing the type, message and stack trace
/// </summary>
public sealed record StrippedException {
	public static readonly StrippedException Default = new();

	/// <summary>
	/// Type of the exception
	/// </summary>
	public readonly string Type;

	/// <summary>
	/// Message of the exception
	/// </summary>
	public readonly string Message;

	/// <summary>
	/// Stack trace of the exception
	/// </summary>
	public readonly string StackTrace;

	/// <summary>
	/// Indicating whether the exception is the default exception (i.e. no exception)
	/// </summary>
	[JsonIgnore]
	public readonly bool IsDefault;

	/// <summary>
	/// Creates a stripped exception from an exception or returns the default
	/// </summary>
	/// <param name="exception"></param>
	/// <returns></returns>
	public static StrippedException FromException(Exception? exception) {
		if (exception is null) {
			return Default;
		}
		return new(exception);
	}

	/// <summary>
	/// Creates a stripped exception from an exception
	/// </summary>
	/// <param name="exception"></param>
	private StrippedException(Exception exception) {
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