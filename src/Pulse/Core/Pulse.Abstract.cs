using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

using Pulse.Configuration;

namespace Pulse.Core;

public abstract class AbstractPulse {
	protected readonly HttpClient _httpClient;
	protected readonly Parameters _parameters;
	protected readonly Func<CancellationToken, Task<Response>> _requestHandler;

	public static AbstractPulse Match(Parameters parameters, RequestDetails requestDetails)
			=> (parameters.ExecutionMode, parameters.BatchSize) switch {
				(ExecutionMode.Sequential, _) => new SequentialPulse(parameters, requestDetails),
				(ExecutionMode.Parallel, Parameters.DefaultBatchSize) => new UnboundedPulse(parameters, requestDetails),
				(ExecutionMode.Parallel, _) => new BoundedPulse(parameters, requestDetails),
				_ => throw new NotImplementedException()
			};

    protected AbstractPulse(Parameters parameters, RequestDetails requestDetails) {
		_parameters = parameters;
		_httpClient = PulseHttpClientFactory.Create(requestDetails);

		bool saveContent = _parameters.Export;
		var requestRecipe = requestDetails.Request;

		_requestHandler = async token => await SendRequest(requestRecipe, _httpClient, saveContent, token);
	}

	/// <summary>
	/// Run the pulse async
	/// </summary>
	/// <param name="requestMessage"></param>
	/// <returns></returns>
	public abstract Task RunAsync(CancellationToken cancellationToken = default);

	private static async Task<Response> SendRequest(Request requestRecipe, HttpClient httpClient,bool saveContent, CancellationToken cancellationToken = default) {
		HttpStatusCode? statusCode = null;
		string? content = null;
		Exception? exception = null;
		HttpResponseHeaders? headers = null;
		int threadId = 0;
		using var message = requestRecipe.CreateMessage();
		var start = Stopwatch.GetTimestamp();
		try {
			threadId = Environment.CurrentManagedThreadId;
			using var response = await httpClient.SendAsync(message, cancellationToken);
			statusCode = response.StatusCode;
			headers = response.Headers;
			if (saveContent) {
				content = await response.Content.ReadAsStringAsync(cancellationToken);
			}
		} catch (Exception e) {
			exception = e;
		} finally {
			message?.Dispose();
		}
		TimeSpan duration = Stopwatch.GetElapsedTime(start);
		return new Response {
			StatusCode = statusCode,
			Headers = headers,
			Content = content,
			Duration = duration,
			Exception = exception is null ? StrippedException.Default : new StrippedException(exception),
			ExecutingThreadId = threadId
		};
	}
}