namespace Pulse.Core;

/// <summary>
/// A resilience pipeline that runs a delegate with jitting, limited with a semaphore
/// </summary>
public sealed class ResiliencePipeline : IDisposable {
	private bool _disposed;
	private readonly SemaphoreSlim _semaphore;

	/// <summary>
	/// Creates a new resilience pipeline
	/// </summary>
	/// <param name="diameter">Concurrent accessors</param>
	public ResiliencePipeline(int diameter) {
		_semaphore = new SemaphoreSlim(diameter, diameter);
	}

	public async Task<TResult> RunAsync<TResult>(Func<CancellationToken, Task<TResult>> @delegate, CancellationToken cancellationToken = default) {
		await _semaphore.WaitAsync(cancellationToken);
		try {
			await Task.Delay(Random.Shared.Next(100), cancellationToken);
			return await @delegate(cancellationToken);
		} finally {
			_semaphore.Release();
		}
	}

    public void Dispose() {
        if (Volatile.Read(ref _disposed)) {
			return;
		}

		_semaphore?.Dispose();
		Volatile.Write(ref _disposed, true);
    }
}