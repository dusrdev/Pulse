namespace Pulse.Configuration;

public sealed class Parameters {
	public bool UseFullEquality { get; set; }

	public readonly CancellationTokenSource CancellationTokenSource = new();
}