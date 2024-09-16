using System.Diagnostics;
using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

public abstract class AbstractPulse : IDisposable {
	protected readonly HttpClient _httpClient;
	protected readonly Parameters _parameters;
	protected readonly Func<CancellationToken, Task<RequestResult>> _requestHandler;
	private readonly ResiliencePipeline? _resiliencePipeline;

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

		HttpRequestMessage message = requestDetails.RequestMessage ?? RequestDetails.Default.RequestMessage!;

		bool saveContent = !_parameters.NoExport;

		if (!_parameters.UseResilience) {
			_requestHandler = async token => await SendRequest(message, saveContent, token);
			return;
		}

		int diameter = _parameters.ConcurrentRequests;
		if (diameter == 1) {
			diameter = Environment.ProcessorCount;
		}
		_resiliencePipeline = new(diameter);
		_requestHandler = async token => await _resiliencePipeline.RunAsync(async _ => await SendRequest(message, saveContent, token), token);
	}


	/// <summary>
	/// Run the pulse async
	/// </summary>
	/// <param name="requestMessage"></param>
	/// <returns></returns>
	public abstract Task RunAsync(CancellationToken cancellationToken = default);

	private async Task<RequestResult> SendRequest(HttpRequestMessage message, bool saveContent, CancellationToken cancellationToken = default) {
		HttpStatusCode? statusCode = null;
		string? content = null;
		Exception? exception = null;
		int threadId = 0;
		var start = Stopwatch.GetTimestamp();
		try {
			var messageCopy = await message.CloneAsync();
			threadId = Environment.CurrentManagedThreadId;
			using var response = await _httpClient.SendAsync(messageCopy, cancellationToken);
			statusCode = response.StatusCode;
			if (saveContent) {
				content = await response.Content.ReadAsStringAsync(cancellationToken);
			}
		} catch (Exception e) {
			exception = e;
		}
		TimeSpan duration = Stopwatch.GetElapsedTime(start);
		return new RequestResult {
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