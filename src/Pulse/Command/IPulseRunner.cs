using Pulse.Configuration;

namespace Pulse.Command;

/// <summary>
/// A pulse contract
/// </summary>
public interface IPulseRunner {
	/// <summary>
	/// Run the pulse async
	/// </summary>
	/// <param name="requestDetails"></param>
	/// <returns></returns>
	public Task<PulseResult> RunAsync(RequestDetails requestDetails);
}

/// <summary>
/// Matches the correct pulse implementation to the config
/// </summary>
public static class PulseRunner {
	public static IPulseRunner Match(Config config) {
		return default;
	}
}