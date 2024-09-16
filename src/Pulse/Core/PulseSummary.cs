using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using System.Runtime.CompilerServices;
using Pulse.Configuration;

namespace Pulse.Core;

public class PulseSummary {
	public required PulseResult Result { get; init; }
	public required Parameters Parameters { get; init; }

	/// <summary>
	/// Produces a summary, and saves unique requests if export is enabled.
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	[MethodImpl(MethodImplOptions.Synchronized)]
	public (bool exportRequired, HashSet<RequestResult>? uniqueRequests) Summarize() {
		HashSet<RequestResult>? uniqueRequests = Parameters.NoExport
												? null
												: new(RequestResultWithExceptionComparer.Singleton);
		Dictionary<HttpStatusCode, int> statusCounter = new();
		double minDuration = double.MaxValue, maxDuration = double.MinValue, avgDuration = 0;
		double multiplier = 1 / (double)Result.TotalCount;

		foreach (var result in Result.Results) {
			uniqueRequests?.Add(result);
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

		return (!Parameters.NoExport, uniqueRequests);
	}


	/// <summary>
	/// Exports unique request results asynchronously and in parallel if possible
	/// </summary>
	/// <param name="uniqueRequests"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public static async Task ExportUniqueRequestsAsync(HashSet<RequestResult> uniqueRequests, CancellationToken token = default) {
		var count = uniqueRequests.Count;

		if (count == 0) {
			WriteLine("No unique results found to export..." * Color.Yellow);
			return;
		} else if (count is 1) {
			await Exporter.ExportHtmlAsync(uniqueRequests.First(), 1, token);
			WriteLine("1" * Color.Cyan, $" unique request exported to ", "results" * Color.Yellow, " folder");
		} else {
			var options = new ParallelOptions {
				MaxDegreeOfParallelism = -1,
				CancellationToken = token
			};

			var indexed = Enumerable.Zip(Enumerable.Range(1, count), uniqueRequests);

			await Parallel.ForEachAsync(
				indexed,
				options,
				async (item, t) => await Exporter.ExportHtmlAsync(item.Second, item.First, t));
			WriteLine(count.ToString() * Color.Cyan, $" unique requests exported to ", "results" * Color.Yellow, " folder");
		}
	}
}