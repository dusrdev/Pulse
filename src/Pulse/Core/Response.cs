using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// The model used for response
/// </summary>
public readonly record struct Response {
	/// <summary>
	/// The id of the request
	/// </summary>
	public required int Id { get; init; }

	/// <summary>
	/// Status code (null if it produced an exception)
	/// </summary>
	public required HttpStatusCode StatusCode { get; init; }

	/// <summary>
	/// Headers (could be null if exception occurred, or server didn't include it)
	/// </summary>
	public required IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; }

	/// <summary>
	/// Content (could be empty if exception occurred, no export feature is used or server didn't include it)
	/// </summary>
	public required string Content { get; init; }

	/// <summary>
	/// The response content length
	/// </summary>
	public required long ContentLength { get; init; }

	/// <summary>
	/// The time taken from sending the request to receiving the response headers
	/// </summary>
	public required TimeSpan Latency { get; init; }

	/// <summary>
	/// The exception (if occurred)
	/// </summary>
	public required StrippedException Exception { get; init; }

	/// <summary>
	/// The current number of concurrent connections at the time of the request
	/// </summary>
	public required int CurrentConcurrentConnections { get; init; }
}

/// <summary>
/// Request comparer to be used in HashSets
/// </summary>
public sealed class ResponseComparer : IEqualityComparer<Response> {
	private readonly Parameters _parameters;

	public ResponseComparer(Parameters parameters) {
		_parameters = parameters;
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
			basicEquality &= x.ContentLength == y.ContentLength;
		}

		if (!basicEquality) {
			return false;
		}

		// Compare Exception types and messages

		if (x.Exception.IsDefault != y.Exception.IsDefault) {
			return false;
		}

		return x.Exception.Message == y.Exception.Message;
	}

	/// <summary>
	/// HashCode doesn't check exception because more complicated equality checks are needed.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public int GetHashCode(Response obj) {
		int hashStatusCode = obj.StatusCode.GetHashCode();

		int hash = 17;
		hash = hash * 23 + hashStatusCode;

		if (obj.Exception.IsDefault) {
			// no exception -> should have content
			hash = hash * 23 + obj.Content.GetHashCode();
		} else {
			// exception = no content (usually)
			hash = hash * 23 + obj.Exception.Message.GetHashCode();
		}

		return hash;
	}
}