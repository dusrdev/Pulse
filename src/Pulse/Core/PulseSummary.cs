using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using System.Runtime.CompilerServices;

namespace Pulse.Core;

public class PulseSummary {
	public required PulseResult Result { get; init; }

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Summarize() {
		HashSet<RequestResult> uniqueRequests = new(RequestResultWithExceptionComparer.Singleton);
		Dictionary<HttpStatusCode, int> statusCounter = new();
		double minDuration = double.MaxValue, maxDuration = double.MinValue, avgDuration = 0;
		double multiplier = 1 / (double)Result.TotalCount;

		foreach (var result in Result.Results) {
			uniqueRequests.Add(result);
			var duration = result.Duration.TotalMilliseconds;
			minDuration = Math.Min(minDuration, duration);
			maxDuration = Math.Max(maxDuration, duration);
			avgDuration += multiplier * duration;

			var statusCode = result.StatusCode ?? 0;
			statusCounter[statusCode] = statusCounter.GetValueOrDefault(statusCode) + 1;
		}

		ClearNextLines(3);
		WriteLine("Statistics:" * Color.Green);
		WriteLine("Success Rate: ", $"{Result.SuccessRate:0.##}%" * Extensions.GetPercentageBasedColor(Result.SuccessRate));
		WriteLine("Request Duration:  Min: ", $"{minDuration:0.##}ms" * Color.Cyan, ", Avg: ", $"{avgDuration:0.##}ms" * Color.Yellow, ", Max: ", $"{maxDuration:0.##}ms" * Color.Red);
		WriteLine("Status codes:");
		foreach (var kvp in statusCounter) {
			var key = (int)kvp.Key;
			if (key is 0) {
				WriteLine("	", $"{key}" * Color.Magenta, $" --> {kvp.Value}	[StatusCode 0 = Exception]");
			} else {
				WriteLine("	", $"{key}" * Extensions.GetStatusCodeBasedColor(key), $" --> {kvp.Value}");
			}
		}
		NewLine();

		int index = 1;
		foreach (var uniqueRequest in uniqueRequests) {
			Exporter.ExportHtml(uniqueRequest, index++);
		}

		int count = index - 1;

		var word = count is 1 ? "result" : "results";

		WriteLine(count.ToString() * Color.Cyan, $" unique {word} exported to ", "results" * Color.Yellow, " folder");
	}
}