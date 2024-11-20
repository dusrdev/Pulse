using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using Pulse.Configuration;
using Sharpify;

namespace Pulse.Core;

/// <summary>
/// Pulse summary handles outputs and experts post-pulse
/// </summary>
public sealed class PulseSummary {
	/// <summary>
	/// The size of the request in bytes
	/// </summary>
	public required long RequestSizeInBytes { get; init; }

	/// <summary>
	/// The pulse result
	/// </summary>
	public required PulseResult Result { get; init; }

	/// <summary>
	/// The pulse parameters
	/// </summary>
	public required Parameters Parameters { get; init; }

	private readonly Lock _lock = new();

	/// <summary>
	/// Produces a summary, and saves unique requests if export is enabled.
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	public (bool exportRequired, HashSet<Response> uniqueRequests) Summarize() {
		var completed = Result.Results.Count;
		if (completed is 1) {
			return SummarizeSingle();
		}

		lock (_lock) {
			HashSet<Response> uniqueRequests = Parameters.Export
													? new HashSet<Response>(new ResponseComparer(Parameters))
													: [];
			Dictionary<HttpStatusCode, int> statusCounter = [];
			var latencies = new List<double>(completed);
			var sizes = new List<double>(completed);
			long totalSize = 0;
			int peakConcurrentConnections = 0;

			var currentLine = GetCurrentLine();

#if !DEBUG
		OverrideCurrentLine(["Cross referencing results..."], OutputPipe.Error);
#endif
			foreach (var result in Result.Results) {
				uniqueRequests.Add(result);
				peakConcurrentConnections = Math.Max(peakConcurrentConnections, result.CurrentConcurrentConnections);
				totalSize += RequestSizeInBytes;
				// duration
				var latency = result.Latency.TotalMilliseconds;
				latencies.Add(latency);
				// size
				var size = result.ContentLength;
				if (size > 0) {
					sizes.Add(size);
					if (Parameters.Export) {
						totalSize += size;
					}
				}

				var statusCode = result.StatusCode;
				statusCounter.GetValueRefOrAddDefault(statusCode, out _)++;
			}
			var latencySummary = GetSummary(latencies.AsSpan());
			var sizeSummary = GetSummary(sizes.AsSpan());
			Func<double, string> getSize = Utils.Strings.FormatBytes;
			double throughput = totalSize / Result.TotalDuration.TotalSeconds;
#if !DEBUG
		OverrideCurrentLine(["Cross referencing results...", " done!" * Color.Green], OutputPipe.Error);
		OverrideCurrentLine([]);
#endif

			if (Parameters.Verbose) {
				NewLine(OutputPipe.Error);
			} else {
				ClearNextLines(3, OutputPipe.Out);
			}

            static string Outliers(int n) => n is 1 ? "outlier" : "outliers";

            WriteLine(["Request count: ", $"{completed}" * Color.Yellow]);
			WriteLine(["Concurrent connections: ", $"{peakConcurrentConnections}" * Color.Yellow]);
			WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
			WriteLine(["Success Rate: ", $"{Result.SuccessRate}%" * Helper.GetPercentageBasedColor(Result.SuccessRate)]);
			Write(["Latency:       Min: ", $"{latencySummary.Min:0.##}ms" * Color.Cyan, ", Avg: ", $"{latencySummary.Avg:0.##}ms" * Color.Yellow, ", Max: ", $"{latencySummary.Max:0.##}ms" * Color.Red]);
			if (latencySummary.Removed is 0) {
				NewLine();
			} else {
				Out.WriteLine($" (Removed {latencySummary.Removed} {Outliers(latencySummary.Removed)})");
			}
			Write(["Content Size:  Min: ", getSize(sizeSummary.Min) * Color.Cyan, ", Avg: ", getSize(sizeSummary.Avg) * Color.Yellow, ", Max: ", getSize(sizeSummary.Max) * Color.Red]);
			if (sizeSummary.Removed is 0) {
				NewLine();
			} else {
				Out.WriteLine($" (Removed {sizeSummary.Removed} {Outliers(sizeSummary.Removed)})");
			}
			WriteLine(["Total throughput: ", $"{getSize(throughput)}/s" * Color.Yellow]);
			Out.WriteLine("Status codes:");
			foreach (var kvp in statusCounter) {
				var key = (int)kvp.Key;
				if (key is 0) {
					WriteLine([$" {key}" * Color.Magenta, $" --> {kvp.Value}	[StatusCode 0 = Exception]"]);
				} else {
					WriteLine([$"	{key}" * Helper.GetStatusCodeBasedColor(key), $" --> {kvp.Value}"]);
				}
			}
			NewLine();

			return (Parameters.Export, uniqueRequests);
		}
	}

	/// <summary>
	/// Produces a summary for a single result
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	public (bool exportRequired, HashSet<Response> uniqueRequests) SummarizeSingle() {
		lock (_lock) {
			var result = Result.Results.First();
			double duration = result.Latency.TotalMilliseconds;
			var statusCode = result.StatusCode;

			if (Parameters.Verbose) {
				NewLine(OutputPipe.Error);
			} else {
				ClearNextLines(3, OutputPipe.Out);
			}

			WriteLine(["Request count: ", "1" * Color.Yellow]);
			WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
			if ((int)statusCode is >= 200 and < 300) {
				WriteLine(["Success: ", "true" * Color.Green]);
			} else {
				WriteLine(["Success: ", "false" * Color.Red]);
			}
			WriteLine(["Latency:      ", $"{duration:0.##}ms" * Color.Cyan]);
			WriteLine(["Content Size: ", Utils.Strings.FormatBytes(result.ContentLength) * Color.Cyan]);
			if (statusCode is 0) {
				WriteLine(["Status code: ", "0 [Exception]" * Color.Red]);
			} else {
				WriteLine(["Status code: ", $"{statusCode}" * Helper.GetStatusCodeBasedColor((int)statusCode)]);
			}
			NewLine();

			var uniqueRequests = new HashSet<Response>(1) { result };

			return (Parameters.Export, uniqueRequests);
		}
	}

	/// <summary>
	/// Creates an IQR summary from <paramref name="values"/>
	/// </summary>
	/// <param name="values"></param>
	/// <returns><see cref="Summary"/></returns>
	internal static Summary GetSummary(Span<double> values) {
		// if conditions ordered to promote default paths
		if (values.Length > 2) {
			values.Sort();

			double q1 = values[values.Length / 4]; // First quartile
			double q3 = values[3 * values.Length / 4]; // Third quartile
			double iqr = q3 - q1;

			double lowerBound = q1 - 1.5 * iqr;
			double upperBound = q3 + 1.5 * iqr;

			var summary = new Summary {
				Min = double.MaxValue,
				Max = double.MinValue,
				Avg = 0
			};

			var filtered = 0;
			foreach (var value in values) {
				if (value < lowerBound || value > upperBound) {
					// Outside of IQR
					summary.Removed++;
					continue;
				}
				filtered++;
				summary.Min = Math.Min(summary.Min, value);
				summary.Max = Math.Max(summary.Max, value);
				summary.Avg += value;
			}
			summary.Avg /= filtered;

			return summary;
		} else if (values.Length is 2) {
			return new Summary {
				Min = Math.Min(values[0], values[1]),
				Max = Math.Max(values[0], values[1]),
				Avg = (values[0] + values[1]) / 2
			};
		} else if (values.Length is 1) {
			return new Summary {
				Min = values[0],
				Max = values[0],
				Avg = values[0]
			};
		} else {
			return new();
		}
	}

	internal struct Summary {
		public double Min;
		public double Max;
		public double Avg;
		public int Removed;
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

		await Parallel.ForEachAsync(uniqueRequests, options, async (request, tkn) => await Exporter.ExportHtmlAsync(request, directory, Parameters.FormatJson, tkn));

		WriteLine([$"{count}" * Color.Cyan, " unique responses exported to ", Parameters.OutputFolder * Color.Yellow, " folder"]);
	}
}