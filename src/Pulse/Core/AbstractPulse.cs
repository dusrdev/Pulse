using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

public abstract class AbstractPulse : IDisposable {
	protected readonly HttpClient _httpClient;
	protected readonly Parameters _parameters;
	protected readonly Func<CancellationToken, Task<Response>> _requestHandler;
	private readonly ResiliencePipeline? _resiliencePipeline;
	protected readonly ConcurrentStack<HttpRequestMessage> _messages;

	private bool _disposed;

	public static AbstractPulse Match(Parameters parameters, RequestDetails requestDetails) {
		return parameters.ConcurrencyMode switch {
			ConcurrencyMode.Maximum => new MaximumPulse(parameters, requestDetails),
			ConcurrencyMode.Limited => new LimitedPulse(parameters, requestDetails),
			ConcurrencyMode.Disabled => new SequentialPulse(parameters, requestDetails),
			_ => throw new InvalidOperationException("ConcurrencyMode doesn't match options")
		};
	}

	protected AbstractPulse(Parameters parameters, RequestDetails requestDetails) {
		_parameters = parameters;
		_httpClient = PulseHttpClientFactory.Create(requestDetails);

		_messages = requestDetails.Request.CreateMessages(_parameters.ConcurrentRequests);

		bool saveContent = !_parameters.NoExport;

		if (!_parameters.UseResilience) {
			_requestHandler = async token => await SendRequest(_messages, _httpClient, saveContent, token);
			return;
		}

		int diameter = _parameters.ConcurrentRequests;
		if (diameter == 1) {
			diameter = Environment.ProcessorCount;
		}
		_resiliencePipeline = new(diameter);
		_requestHandler = async token => await _resiliencePipeline.RunAsync(async _ => await SendRequest(_messages, _httpClient, saveContent, token), token);
	}

	/// <summary>
	/// Run the pulse async
	/// </summary>
	/// <param name="requestMessage"></param>
	/// <returns></returns>
	public abstract Task RunAsync(CancellationToken cancellationToken = default);

	private static async Task<Response> SendRequest(ConcurrentStack<HttpRequestMessage> messages, HttpClient httpClient,bool saveContent, CancellationToken cancellationToken = default) {
		HttpStatusCode? statusCode = null;
		string? content = null;
		Exception? exception = null;
		int threadId = 0;
		if (!messages.TryPop(out var message)) {
			throw new Exception("Failed to pop message from stack");
		}
		var start = Stopwatch.GetTimestamp();
		try {
			threadId = Environment.CurrentManagedThreadId;
			using var response = await httpClient.SendAsync(message, cancellationToken);
			statusCode = response.StatusCode;
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
			Content = content,
			Duration = duration,
			Exception = exception,
			ExecutingThreadId = threadId
		};
	}

    public void Dispose() {
        if (Volatile.Read(ref _disposed)) {
			return;
		}
		_resiliencePipeline?.Dispose();
		Volatile.Write(ref _disposed, true);
		GC.SuppressFinalize(this);
    }
}