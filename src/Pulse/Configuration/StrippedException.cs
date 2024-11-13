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
		return new StrippedException(exception);
	}

	/// <summary>
	/// Creates a stripped exception from an exception
	/// </summary>
	/// <param name="exception"></param>
	private StrippedException(Exception exception) {
		Type = exception.GetType().Name;
		Message = exception.Message;
		IsDefault = false;
	}

	/// <summary>
	/// Creates a stripped exception from a type, message and stack trace
	/// </summary>
	/// <param name="type"></param>
	/// <param name="message"></param>
	/// <param name="stackTrace"></param>
	public StrippedException(string type, string message) {
		Type = type;
		Message = message;
		IsDefault = false;
	}

	[JsonConstructor]
	public StrippedException() {
		Type = string.Empty;
		Message = string.Empty;
		IsDefault = true;
	}
}