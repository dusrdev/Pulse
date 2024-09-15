using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;

namespace Pulse.Core;

public readonly struct PulseSummary {
	public required PulseResult Result { get; init; }

	public void Summarize() {
		HashSet<RequestResult> uniqueRequests = new();
		Dictionary<HttpStatusCode, int> statusCounter = new();
		double minDuration = 0, maxDuration = 0, avgDuration = 0;
		double multiplier = 1 / Result.TotalCount;

		foreach (var result in Result.Results) {
			uniqueRequests.Add(result);
			var duration = result.Duration.TotalMilliseconds;
			minDuration = Math.Min(minDuration, duration);
			maxDuration = Math.Max(maxDuration, duration);
			avgDuration += multiplier * duration;
			statusCounter[result.StatusCode ?? 0]++;
		}

		WriteLine("Statistics:" * Color.Green);
		NewLine();
		WriteLine("Request Duration:		Min		Avg		Max");
		WriteLine("							", $"{minDuration:.##}" * Color.Yellow, "		", $"{avgDuration:.##}", "		",
		$"{minDuration:.##}");
		NewLine();
		WriteLine("Status codes:");
		foreach (var kvp in statusCounter) {
			WriteLine("	", $"{kvp.Key}" * Color.Yellow, $" --> {kvp.Value}");
		}
		NewLine();

		//TODO: export
	}
}