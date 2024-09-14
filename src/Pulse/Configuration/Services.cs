namespace Pulse.Configuration;

public sealed class Services : IDisposable {
	private bool _disposed;

	/// <summary>
	/// Singleton instance
	/// </summary>
	public static readonly Services Instance = new();

	private Services() {
	}

	public void Dispose() {
		if (Volatile.Read(ref _disposed)) {
			return;
		}

		// Dispose here

		Volatile.Write(ref _disposed, true);
	}
}
