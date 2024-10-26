namespace Pulse.Configuration;

public sealed class Services {
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static Services Shared { get; set; } = default!;

	/// <summary>
	/// The global parameters for the execution scope
	/// </summary>
	public readonly Parameters Parameters;

	public Services(Parameters parameters) {
		Parameters = parameters;
	}
}
