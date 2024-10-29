namespace Pulse.Configuration;

public sealed class Services {
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static Services Shared { get; set; } = default!;

	/// <summary>
	/// The global parameters for the execution scope
	/// </summary>
	public Parameters Parameters { get; private set; }

	public Services(Parameters parameters) {
		Parameters = parameters;
	}

	public void OverrideParameters(Parameters parameters) {
		Parameters current = Parameters;
		Parameters = parameters;
		current.Dispose();
	}
}
