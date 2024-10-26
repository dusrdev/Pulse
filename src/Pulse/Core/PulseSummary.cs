using System.Net;
using static PrettyConsole.Console;
using PrettyConsole;
using System.Runtime.CompilerServices;
using Pulse.Configuration;
using Sharpify;
using Sharpify.Collections;
using System.Text;

namespace Pulse.Core;

public class PulseSummary {
	public required PulseResult Result { get; init; }
	public required Parameters Parameters { get; init; }
	public Encoding CharEncoding { get; init; } = Encoding.Default;

	/// <summary>
	/// Produces a summary, and saves unique requests if export is enabled.
	/// </summary>
	/// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
	[MethodImpl(MethodImplOptions.Synchronized)]
	public (bool exportRequired, HashSet<Response> uniqueRequests) Summarize() {
		if (Result.Results.Count == 1) {
			return SummarizeSingle();
		}

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
		var prg = new ProgressBar {
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
			ReadOnlySpan<char> span = result.Content ?? ReadOnlySpan<char>.Empty;
			var size = CharEncoding.GetByteCount(span);
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
		minSize = Math.Min(minSize, maxSize);

		System.Console.SetCursorPosition(0, cursorTop);

		Func<double, string> getSize = Utils.Strings.FormatBytes;

		ClearNextLinesError(3);
		WriteLine("Summary:" * Color.Green);
		WriteLine(["Request count: ", $"{Parameters.Requests}" * Color.Yellow]);
		WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
		if (Parameters.Verbose) {
			WriteLine(["Threads used: ", $"{uniqueThreadIds.Count}" * Color.Yellow]);
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
		ReadOnlySpan<char> span = result.Content ?? ReadOnlySpan<char>.Empty;
		var size = CharEncoding.GetByteCount(span);

		var statusCode = result.StatusCode ?? 0;
		ClearNextLinesError(3);
		WriteLine("Summary:" * Color.Green);
		WriteLine(["Request count: ", "1" * Color.Yellow]);
		WriteLine(["Total duration: ", Utils.DateAndTime.FormatTimeSpan(Result.TotalDuration) * Color.Yellow]);
		if (Parameters.Verbose) {
			WriteLine(["Threads used: ", "1" * Color.Yellow]);
			WriteLine(["RAM Consumed: ", Utils.Strings.FormatBytes(Result.MemoryUsed) * Color.Yellow]);
		}
		if (statusCode is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous) {
			WriteLine(["Success: ", "true" * Color.Green]);
		} else {
			WriteLine(["Success: ", "false" * Color.Red]);
		}
		WriteLine(["Request Duration: ", $"{duration:0.##}ms" * Color.Cyan]);
		WriteLine(["Content Size: ", Utils.Strings.FormatBytes(size) * Color.Cyan]);
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

			int index = 1;

			var total = uniqueRequests.Count;
            var batchSize = Environment.ProcessorCount;
			using var enumerator = uniqueRequests.GetEnumerator();

			do {
				var batch = Math.Min(batchSize, total);

				using var buffer = new RentedBufferWriter<Task>(batch);

				for (int i = 0; i < batch; i++) {
					buffer.WriteAndAdvance(Task.Run(() => Exporter.ExportHtmlAsync(enumerator.Current, directory, index++, token), token));
					if (!enumerator.MoveNext()) {
						break;
					}
				}

				var tasks = buffer.WrittenSegment;
				await Task.WhenAll(tasks).WaitAsync(token).ConfigureAwait(false);

				total -= batch;
			} while (total > 0 && !token.IsCancellationRequested);

			WriteLine([count.ToString() * Color.Cyan, " unique response exported to ", "results" * Color.Yellow, " folder"]);
		}
	}
}