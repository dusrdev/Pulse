using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// The model used for response
/// </summary>
public readonly struct Response {
	/// <summary>
	/// Status code (null if it produced an exception)
	/// </summary>
	public required HttpStatusCode? StatusCode { get; init; }

	/// <summary>
	/// Content (could be null if exception occurred, no export feature is used or server didn't include it)
	/// </summary>
	public required string? Content { get; init; }

	/// <summary>
	/// The time taken from sending the request to receiving the response
	/// </summary>
	public required TimeSpan Duration { get; init; }

	/// <summary>
	/// The exception (if occurred)
	/// </summary>
	public required Exception? Exception { get; init; }

	/// <summary>
	/// The id of the thread that executed the request
	/// </summary>
	public required int ExecutingThreadId { get; init; }
}

/// <summary>
/// Request comparer to be used in HashSets
/// </summary>
public sealed class RequestResultWithExceptionComparer : IEqualityComparer<Response> {
	public static readonly RequestResultWithExceptionComparer Singleton = new(Services.Instance.Parameters);

	private readonly Parameters _parameters;

	private RequestResultWithExceptionComparer(Parameters parameters) {
		_parameters = parameters;
	}

	public bool Equals(Response? x, Response? y) {
		if (x is null || y is null) {
			return false;
		}

		return Equals(x.Value, y.Value);
	}

	/// <summary>
	/// Equality here does not take into account all properties as some are not valuable for the response itself,
	/// for instance ThreadId doesn't matter for response equality, neither duration.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <remarks>
	/// If <see cref="ParametersBase.UseFullEquality"/> is not used, the content is only checked by length to accelerate checks, generally this is sufficient as usually a website won't return same length responses for different results
	/// </remarks>
	/// <returns></returns>
    public bool Equals(Response x, Response y) { // Equals is only used if HashCode is equal
		bool basicEquality = x.StatusCode == y.StatusCode;

		if (_parameters.UseFullEquality) {
			basicEquality &= string.Equals(x.Content, y.Content, StringComparison.Ordinal);
		} else {
			bool contentsEqual = x.Content is not null
								&& y.Content is not null
								&& x.Content.Length == y.Content.Length;
			basicEquality &= contentsEqual;
		}

		if (!basicEquality) {
			return false;
		}

		if (x.Exception == null && y.Exception == null) {
			return true;
		}

		if (x.Exception == null || y.Exception == null) {
			return false;
		}

		// Compare Exception types and messages
		return x.Exception.GetType() == y.Exception.GetType() &&
			   string.Equals(x.Exception.Message, y.Exception.Message, StringComparison.Ordinal);
    }

	/// <summary>
	/// HashCode doesn't check exception because more complicated equality checks are needed.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
    public int GetHashCode(Response obj) {
		int hashStatusCode = obj.StatusCode.HasValue ? obj.StatusCode.Value.GetHashCode() : 0;

		int hash = 17;
		hash = hash * 23 + hashStatusCode;

		if (_parameters.UseFullEquality) {
			int hashContent = obj.Content != null ? obj.Content.GetHashCode() : 0;
			hash = hash * 23 + hashContent;
		}

		return hash;
	}
}