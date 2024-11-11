using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using System.Runtime.CompilerServices;
using Pulse.Configuration;
using Sharpify;
using System.Text;

namespace Pulse.Core;

/// <summary>
/// Pulse summary handles outputs and experts post-pulse
/// </summary>
public sealed class PulseSummary {
	/// <summary>
	/// The pulse result
	/// </summary>
	public required PulseResult Result { get; init; }

	/// <summary>
	/// The pulse parameters
	/// </summary>
	public required Parameters Parameters { get; init; }

	/// <summary>
	/// Produces a summary, and saves unique requests if export is enabled.
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	[MethodImpl(MethodImplOptions.Synchronized)]
	public (bool exportRequired, HashSet<Response> uniqueRequests) Summarize() {
		var completed = Result.Results.Count;
		if (completed is 1) {
			return SummarizeSingle();
		}

		HashSet<Response> uniqueRequests = Parameters.Export
												? new(new ResponseComparer(Parameters))
												: [];
		Dictionary<HttpStatusCode, int> statusCounter = [];
		double minDuration = double.MaxValue, maxDuration = double.MinValue, avgDuration = 0;
		double minSize = double.MaxValue, maxSize = double.MinValue, avgSize = 0;
		double multiplier = 1 / (double)completed;
		int maximumConcurrencyLevel = 0;
		int total = completed;
		int current = 0;
#if !DEBUG
		var prg = new ProgressBar {
			ProgressColor = Color.Yellow,
		};
#endif

		var cursorTop = System.Console.CursorTop;

		foreach (var result in Result.Results) {
			uniqueRequests.Add(result);
			maximumConcurrencyLevel = Math.Max(maximumConcurrencyLevel, result.MaximumConcurrencyLevel);
			// duration
			var duration = result.Duration.TotalMilliseconds;
			minDuration = Math.Min(minDuration, duration);
			maxDuration = Math.Max(maxDuration, duration);
			avgDuration += multiplier * duration;
			// size
			var size = result.ContentLength;
			if (size > 0) {
				minSize = Math.Min(minSize, size);
				maxSize = Math.Max(maxSize, size);
				avgSize += multiplier * size;
			}

			var statusCode = result.StatusCode;
			statusCounter.GetValueRefOrAddDefault(statusCode, out _)++;

			// prg part
			current++;
			double percentage = 100 * (double)current / total;
#if !DEBUG
			prg.Update(percentage, "Cross referencing results...");
#endif
		}

		maxSize = Math.Max(0, maxSize);
		minSize = Math.Min(minSize, maxSize);

		System.Console.SetCursorPosition(0, cursorTop);

		Func<double, string> getSize = Utils.Strings.FormatBytes;

		ClearNextLinesError(3);
		WriteLine("Summary:" * Color.Green);
		WriteLine(["Request count: ", $"{completed}" * Color.Yellow]);
		WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
		if (Parameters.Verbose) {
			WriteLine(["Maximum concurrent connections: ", $"{maximumConcurrencyLevel}" * Color.Yellow]);
			WriteLine(["RAM Consumed: ", getSize(Result.MemoryUsed) * Color.Yellow]);
		}
		WriteLine(["Success Rate: ", $"{Result.SuccessRate}%" * Extensions.GetPercentageBasedColor(Result.SuccessRate)]);
		WriteLine(["Request Duration:  Min: ", $"{minDuration:0.##}ms" * Color.Cyan, ", Avg: ", $"{avgDuration:0.##}ms" * Color.Yellow, ", Max: ", $"{maxDuration:0.##}ms" * Color.Red]);
		WriteLine(["Content Size:  Min: ", getSize(minSize) * Color.Cyan, ", Avg: ", getSize(avgSize) * Color.Yellow, ", Max: ", getSize(maxSize) * Color.Red]);
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
	/// Produces a summary for a single result
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	[MethodImpl(MethodImplOptions.Synchronized)]
	public (bool exportRequired, HashSet<Response> uniqueRequests) SummarizeSingle() {
		var result = Result.Results.First();
		double duration = result.Duration.TotalMilliseconds;
		var statusCode = result.StatusCode;

		ClearNextLinesError(3);
		WriteLine("Summary:" * Color.Green);
		WriteLine(["Request count: ", "1" * Color.Yellow]);
		WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
		if (Parameters.Verbose) {
			WriteLine(["Maximum concurrent connections: ", $"{result.MaximumConcurrencyLevel}" * Color.Yellow]);
			WriteLine(["RAM Consumed: ", Utils.Strings.FormatBytes(Result.MemoryUsed) * Color.Yellow]);
		}
		if ((int)statusCode is >= 200 and < 300) {
			WriteLine(["Success: ", "true" * Color.Green]);
		} else {
			WriteLine(["Success: ", "false" * Color.Red]);
		}
		WriteLine(["Request Duration: ", $"{duration:0.##}ms" * Color.Cyan]);
		WriteLine(["Content Size: ", Utils.Strings.FormatBytes(result.ContentLength) * Color.Cyan]);
		if (statusCode is 0) {
			WriteLine(["Status code: ", "0 [Exception]" * Color.Red]);
		} else {
			WriteLine(["Status code: ", $"{statusCode}" * Extensions.GetStatusCodeBasedColor((int)statusCode)]);
		}
		NewLine();

		var uniqueRequests = new HashSet<Response>(1) { result };

		return (Parameters.Export, uniqueRequests);
	}

	/// <summary>
	/// Exports unique request results asynchronously and in parallel if possible
	/// </summary>
	/// <param name="uniqueRequests"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public async Task ExportUniqueRequestsAsync(HashSet<Response> uniqueRequests, CancellationToken token = default) {
		var count = uniqueRequests.Count;

		if (count is 0) {
			WriteLine("No unique results found to export..." * Color.Yellow);
			return;
		}

		string directory = Path.Join(Directory.GetCurrentDirectory(), Parameters.OutputFolder);
		Directory.CreateDirectory(directory);
		Exporter.ClearFiles(directory);

		if (count is 1) {
			await Exporter.ExportHtmlAsync(uniqueRequests.First(), directory, Parameters.FormatJson, token);
			WriteLine(["1" * Color.Cyan, $" unique response exported to ", Parameters.OutputFolder * Color.Yellow, " folder"]);
			return;
		}

		var options = new ParallelOptions {
			MaxDegreeOfParallelism = Environment.ProcessorCount,
			CancellationToken = token
		};

		await Parallel.ForEachAsync(uniqueRequests, options, async (request, token) => await Exporter.ExportHtmlAsync(request, directory, Parameters.FormatJson, token));

		WriteLine([$"{count}" * Color.Cyan, " unique responses exported to ", Parameters.OutputFolder * Color.Yellow, " folder"]);
	}
}