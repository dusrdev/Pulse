using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

public readonly struct RequestResult {
	public required HttpStatusCode? StatusCode { get; init; }
	public required string? Content { get; init; }
	public required TimeSpan Duration { get; init; }
	public required Exception? Exception { get; init; }
	public required int ExecutingThreadId { get; init; }
}

public sealed class RequestResultWithExceptionComparer : IEqualityComparer<RequestResult> {
	public static readonly RequestResultWithExceptionComparer Singleton = new(Services.Instance.Parameters);

	private readonly Parameters _parameters;

	private RequestResultWithExceptionComparer(Parameters parameters) {
		_parameters = parameters;
	}

	public bool Equals(RequestResult? x, RequestResult? y) {
		if (x is null || y is null) {
			return false;
		}

		return Equals(x.Value, y.Value);
	}

    public bool Equals(RequestResult x, RequestResult y) {
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

    public int GetHashCode(RequestResult obj) {
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