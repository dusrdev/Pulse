using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using Pulse.Configuration;
using Sharpify;
using System.Numerics;
using Sharpify.Collections;
using System.Runtime.Intrinsics;

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
			var latencies = new double[completed];
			var latencyManager = BufferWrapper<double>.Create(latencies);
			var sizes = new double[completed];
			var sizeManager = BufferWrapper<double>.Create(sizes);

			long totalSize = 0;
			int peakConcurrentConnections = 0;

			var currentLine = GetCurrentLine();

#if !DEBUG
		OverrideCurrentLine(["Cross referencing results..."], OutputPipe.Error);
#endif
			foreach (var result in Result.Results) {
				uniqueRequests.Add(result);
				var statusCode = result.StatusCode;
				statusCounter.GetValueRefOrAddDefault(statusCode, out _)++;
				totalSize += RequestSizeInBytes;
				peakConcurrentConnections = Math.Max(peakConcurrentConnections, result.CurrentConcurrentConnections);

				if (!result.Exception.IsDefault) {
					continue;
				}

				// duration
				var latency = result.Latency.TotalMilliseconds;
				latencyManager.Append(latency);
				// size
				var size = result.ContentLength;
				if (size > 0) {
					sizeManager.Append(size);
					if (Parameters.Export) {
						totalSize += size;
					}
				}
			}
			Summary latencySummary = GetSummary(latencies.AsSpan(0, latencyManager.Position));
			Summary sizeSummary = GetSummary(sizes.AsSpan(0, sizeManager.Position), false);
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
			Write(["Latency:       Min: ", $"{latencySummary.Min:0.##}ms" * Color.Cyan, ", Mean: ", $"{latencySummary.Mean:0.##}ms" * Color.Yellow, ", Max: ", $"{latencySummary.Max:0.##}ms" * Color.Red]);
			if (latencySummary.Removed is 0) {
				NewLine();
			} else {
				Out.WriteLine($" (Removed {latencySummary.Removed} {Outliers(latencySummary.Removed)})");
			}
			WriteLine(["Content Size:  Min: ", getSize(sizeSummary.Min) * Color.Cyan, ", Mean: ", getSize(sizeSummary.Mean) * Color.Yellow, ", Max: ", getSize(sizeSummary.Max) * Color.Red]);
			WriteLine(["Total throughput: ", $"{getSize(throughput)}/s" * Color.Yellow]);
			Out.WriteLine("Status codes:");
			foreach (var kvp in statusCounter.OrderBy(static s => (int)s.Key)) {
				var key = (int)kvp.Key;
				if (key is 0) {
					WriteLine([$"   {key}" * Color.Magenta, $" --> {kvp.Value}  [StatusCode 0 = Exception]"]);
				} else {
					WriteLine([$"   {key}" * Helper.GetStatusCodeBasedColor(key), $" --> {kvp.Value}"]);
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
	internal static Summary GetSummary(Span<double> values, bool removeOutliers = true) {
		// if conditions ordered to promote default paths
		if (values.Length > 2) {
			values.Sort();

			if (!removeOutliers) {
				return SummarizeOrderedSpan(values, 0);
			}

			int i25 = values.Length / 4, i75 = 3 * values.Length / 4;
			double q1 = values[i25]; // First quartile
			double q3 = values[i75]; // Third quartile
			double iqr = q3 - q1;
			double lowerBound = q1 - 1.5 * iqr;
			double upperBound = q3 + 1.5 * iqr;

			int start = FindBoundIndex(values, lowerBound, 0, i25);
			int end = FindBoundIndex(values, upperBound, i75, values.Length);
			ReadOnlySpan<double> filtered = values.Slice(start, end - start);

			return SummarizeOrderedSpan(filtered, values.Length - filtered.Length);
		} else if (values.Length is 2) {
			return new Summary {
				Min = Math.Min(values[0], values[1]),
				Max = Math.Max(values[0], values[1]),
				Mean = (values[0] + values[1]) / 2
			};
		} else if (values.Length is 1) {
			return new Summary {
				Min = values[0],
				Max = values[0],
				Mean = values[0]
			};
		} else {
			return new();
		}
	}

	internal static int FindBoundIndex(ReadOnlySpan<double> orderedValues, double bound, int clampMin, int clampMax) {
		int index = orderedValues.BinarySearch(bound);
		if (index < 0) {
			index = ~index; // Get the insertion point
		}
		return Math.Clamp(index, clampMin, clampMax);
	}

	internal static Summary SummarizeOrderedSpan(ReadOnlySpan<double> values, int removed) {
		return new Summary {
			Min = values[0],
			Max = values[values.Length - 1],
			Mean = Mean(values),
			Removed = removed
		};
	}

	internal struct Summary {
		public double Min;
		public double Max;
		public double Mean;
		public int Removed;
	}

	internal static double Mean(ReadOnlySpan<double> span) {
		double mean = 0;
		double reciprocal = 1.0 / span.Length;
		int i = 0;

		// Process data in chunks of vectorSize
		if (Vector512.IsHardwareAccelerated) {
			int vectorSize = Vector512<double>.Count;
            var r = Vector512.Create(reciprocal);
			while (i <= span.Length - vectorSize) {
				var vector = Vector512.Create(span.Slice(i, vectorSize));
                var product = Vector512.Multiply(vector, r);
                mean += Vector512.Sum(product);
				i += vectorSize;
			}
		} else {
			int vectorSize = Vector<double>.Count;
            var r = Vector.Create(reciprocal);
			while (i <= span.Length - vectorSize) {
				var vector = Vector.Create(span.Slice(i, vectorSize));
				var product = Vector.Multiply(vector, r);
                mean += Vector.Sum(product);
				i += vectorSize;
			}
		}

		// Process remaining elements
		double scalerSum = 0;
		for (; i < span.Length; i++) {
			scalerSum += span[i];
		}
		mean += scalerSum * reciprocal;

		return mean;
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
			await Exporter.ExportResponseAsync(uniqueRequests.First(), directory, Parameters, token);
			WriteLine(["1" * Color.Cyan, $" unique response exported to ", Parameters.OutputFolder * Color.Yellow, " folder"]);
			return;
		}

		var options = new ParallelOptions {
			MaxDegreeOfParallelism = Environment.ProcessorCount,
			CancellationToken = token
		};

		await Parallel.ForEachAsync(uniqueRequests, options, async (request, tkn) => await Exporter.ExportResponseAsync(request, directory, Parameters, tkn));

		WriteLine([$"{count}" * Color.Cyan, " unique responses exported to ", Parameters.OutputFolder * Color.Yellow, " folder"]);
	}
}