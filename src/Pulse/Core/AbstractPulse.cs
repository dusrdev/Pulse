using System.Diagnostics;
using System.Net;

using Pulse.Configuration;

namespace Pulse.Core;

public abstract class AbstractPulse : IDisposable {
	protected readonly HttpClient _httpClient;
	protected readonly Config _config;
	protected readonly Func<CancellationToken, Task<RequestResult>> _requestHandler;
	private readonly ResiliencePipeline? _resiliencePipeline;

	private bool _disposed;

	public static AbstractPulse Match(Config config, RequestDetails requestDetails) {
		return config.ConcurrencyMode switch {
			ConcurrencyMode.Maximum => new MaximumPulse(config, requestDetails),
			ConcurrencyMode.Limited => new LimitedPulse(config, requestDetails),
			ConcurrencyMode.Disabled => new SequentialPulse(config, requestDetails),
			_ => throw new InvalidOperationException("ConcurrencyMode doesn't match options")
		};
	}

	protected AbstractPulse(Config config, RequestDetails requestDetails) {
		_config = config;
		_httpClient = PulseHttpClientFactory.Create(requestDetails);

		HttpRequestMessage message = requestDetails.RequestMessage ?? RequestDetails.Default.RequestMessage!;

		if (!config.UseResilience) {
			_requestHandler = async token => await SendRequest(message, token);
			return;
		}

		int diameter = config.ConcurrentRequests;
		if (diameter == 1) {
			diameter = Environment.ProcessorCount;
		}
		_resiliencePipeline = new(diameter);
		_requestHandler = async token => await _resiliencePipeline.RunAsync(async _ => await SendRequest(message, token), token);
	}


	/// <summary>
	/// Run the pulse async
	/// </summary>
	/// <param name="requestMessage"></param>
	/// <returns></returns>
	public abstract Task RunAsync(CancellationToken cancellationToken = default);

	private async Task<RequestResult> SendRequest(HttpRequestMessage message, CancellationToken cancellationToken = default) {
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
			content = await response.Content.ReadAsStringAsync(cancellationToken);
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