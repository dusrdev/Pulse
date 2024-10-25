using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using System.Runtime.CompilerServices;
using Pulse.Configuration;
using Sharpify;

namespace Pulse.Core;

public class PulseSummary {
	public required PulseResult Result { get; init; }
	public required Parameters Parameters { get; init; }

	/// <summary>
	/// Produces a summary, and saves unique requests if export is enabled.
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	[MethodImpl(MethodImplOptions.Synchronized)]
	public (bool exportRequired, HashSet<Response> uniqueRequests) Summarize() {
		HashSet<Response> uniqueRequests = Parameters.Export
												? new(ResponseWithExceptionComparer.Singleton)
												: [];
		Dictionary<HttpStatusCode, int> statusCounter = [];
		HashSet<int> uniqueThreadIds = [];
		double minDuration = double.MaxValue, maxDuration = double.MinValue, avgDuration = 0;
		double minSize = double.MaxValue, maxSize = double.MinValue, avgSize = 0;
		double multiplier = 1 / (double)Result.TotalCount;
		int total = Parameters.Requests;
		int current = 0;
		var prg = new ProgressBar() {
			ProgressColor = Color.Yellow,
		};

		var cursorTop = System.Console.CursorTop;

		foreach (var result in Result.Results) {
			uniqueRequests.Add(result);
			uniqueThreadIds.Add(result.ExecutingThreadId);
			// duration
			var duration = result.Duration.TotalMilliseconds;
			minDuration = Math.Min(minDuration, duration);
			maxDuration = Math.Max(maxDuration, duration);
			avgDuration += multiplier * duration;
			// size
			var size = (double)(result.Content?.Length ?? 0) * sizeof(char) / sizeof(byte);
			if (size > 0) {
				minSize = Math.Min(minSize, size);
				maxSize = Math.Max(maxSize, size);
				avgSize += multiplier * size;
			}

			var statusCode = result.StatusCode ?? 0;
			statusCounter.GetValueRefOrAddDefault(statusCode, out _)++;

			// prg part
			current++;
			double percentage = 100 * (double)current / total;
			prg.Update(percentage, "Cross referencing results...");
		}

		maxSize = Math.Max(0, maxSize);
		minSize = Math.Max(0, minSize);

		System.Console.SetCursorPosition(0, cursorTop);

		ClearNextLinesError(3);
		WriteLine("Statistics:" * Color.Green);
		WriteLine(["Request count: ", $"{Parameters.Requests}" * Color.Yellow]);
		WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
		WriteLine(["Threads used: ", $"{uniqueThreadIds.Count}" * Color.Yellow]);
		WriteLine(["RAM Consumed: ", Utils.Strings.FormatBytes(Result.MemoryUsed) * Color.Yellow]);
		WriteLine(["Success Rate: ", $"{Result.SuccessRate}%" * Extensions.GetPercentageBasedColor(Result.SuccessRate)]);
		WriteLine(["Request Duration:  Min: ", $"{minDuration:0.##}ms" * Color.Cyan, ", Avg: ", $"{avgDuration:0.##}ms" * Color.Yellow, ", Max: ", $"{maxDuration:0.##}ms" * Color.Red]);
		WriteLine(["Content Size:  Min: ", Utils.Strings.FormatBytes(minSize) * Color.Cyan, ", Avg: ", Utils.Strings.FormatBytes(avgSize) * Color.Yellow, ", Max: ", Utils.Strings.FormatBytes(maxSize) * Color.Red]);
		WriteLine("Status codes:");
		foreach (var kvp in statusCounter) {
			var key = (int)kvp.Key;
			if (key is 0) {
				WriteLine([$" {key}" * Color.Magenta, $" --> {kvp.Value}	[StatusCode 0 = Exception]"]);
			} else {
				WriteLine([$"	{key}" * Extensions.GetStatusCodeBasedColor(key), $" --> {kvp.Value}"]);
			}
		}
		NewLine();

		return (Parameters.Export, uniqueRequests);
	}


	/// <summary>
	/// Exports unique request results asynchronously and in parallel if possible
	/// </summary>
	/// <param name="uniqueRequests"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public static async Task ExportUniqueRequestsAsync(HashSet<Response> uniqueRequests, CancellationToken token = default) {
		var count = uniqueRequests.Count;

		if (count == 0) {
			WriteLine("No unique results found to export..." * Color.Yellow);
			return;
		}

		string directory = Path.Join(Directory.GetCurrentDirectory(), "results/");
		Directory.CreateDirectory(directory);
		Exporter.ClearFiles(directory);

		if (count == 1) {
			await Exporter.ExportHtmlAsync(uniqueRequests.First(), directory, 1, token);
			WriteLine(["1" * Color.Cyan, $" unique response exported to ", "results" * Color.Yellow, " folder"]);
		} else {
			var options = new ParallelOptions {
				MaxDegreeOfParallelism = -1,
				CancellationToken = token
			};

			var indexed = Enumerable.Zip(uniqueRequests, Enumerable.Range(1, count));

			await Parallel.ForEachAsync(
				indexed,
				options,
				async (item, t) => await Exporter.ExportHtmlAsync(item.First, directory, item.Second, t));
			WriteLine([count.ToString() * Color.Cyan, " unique response exported to ", "results" * Color.Yellow, " folder"]);
		}
	}
}