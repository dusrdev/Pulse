using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

/// <summary>
/// Pulse summary handles outputs and experts post-pulse
/// </summary>
internal static class PulseSummary {
    /// <summary>
    /// Produces a summary, and saves unique requests if export is enabled.
    /// </summary>
    /// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
    public static (bool exportRequired, HashSet<Response> uniqueRequests) Summarize(Parameters parameters, PulseResult pulseResult, long requestSizeInBytes) {
        var completed = pulseResult.Results.Count;
        if (completed is 1) {
            return SummarizeSingle(parameters, pulseResult);
        }

        HashSet<Response> uniqueRequests = parameters.Export
                                                ? new HashSet<Response>(new ResponseComparer(parameters))
                                                : [];
        Dictionary<HttpStatusCode, int> statusCounter = [];
        var latencies = new List<double>(completed);
        var sizes = new List<double>(completed);

        long totalSize = 0;
        int peakConcurrentConnections = 0;

        ConsoleState.LinesWritten += 1;
        Overwrite(() => {
            Write(OutputPipe.Error, $"Cross referencing results...");
        }, 1, OutputPipe.Error);

        foreach (var result in pulseResult.Results) {
            uniqueRequests.Add(result);
            var statusCode = result.StatusCode;
            statusCounter.GetValueRefOrAddDefault(statusCode, out _)++;
            totalSize += requestSizeInBytes;
            peakConcurrentConnections = Math.Max(peakConcurrentConnections, result.CurrentConcurrentConnections);

            if (!result.Exception.IsDefault) {
                continue;
            }

            // duration
            var latency = result.Latency.TotalMilliseconds;
            latencies.Add(latency);
            // size
            var size = result.ContentLength;
            if (size > 0) {
                sizes.Add(size);
                if (parameters.Export) {
                    totalSize += size;
                }
            }
        }
        Summary latencySummary = GetSummary(CollectionsMarshal.AsSpan(latencies));
        Summary sizeSummary = GetSummary(CollectionsMarshal.AsSpan(sizes), false);
        Func<double, string> getSize = Utils.Strings.FormatBytes;
        double throughput = totalSize / pulseResult.TotalDuration.TotalSeconds;

        // Clear "cross referencing results..."
        ClearNextLines(1, OutputPipe.Error);

        WriteLine($"Request count: {Yellow}{completed}");
        WriteLine($"Concurrent connections: {Yellow}{peakConcurrentConnections}");
        WriteLine($"Total duration: {Yellow}{pulseResult.TotalDuration:hr}");
        WriteLine($"Success Rate: {Helper.GetPercentageBasedColor(pulseResult.SuccessRate)}{pulseResult.SuccessRate}%");
        WriteLine($"Latency:       Min: {Green}{latencySummary.Min:0.##}ms{Default}, Mean: {Yellow}{latencySummary.Mean:0.##}ms{Default}, Max: {Red}{latencySummary.Max:0.##}ms");
        if (latencySummary.Removed != 0) {
            WriteLine($"               (Removed {DarkYellow}{latencySummary.Removed}{Default} {(latencySummary.Removed == 1 ? "outlier" : "outliers")})");
        }
        WriteLine($"Content Size:  Min: {Green}{getSize(sizeSummary.Min)}{Default}, Mean: {Yellow}{getSize(sizeSummary.Mean)}{Default}, Max: {Red}{getSize(sizeSummary.Max)}");
        WriteLine($"Total throughput: {Yellow}{getSize(throughput)}/s");
        WriteLine($"Status codes:");
        foreach (var kvp in statusCounter.OrderBy(static s => (int)s.Key)) {
            var key = (int)kvp.Key;
            if (key is 0) {
                WriteLine($"   {Magenta}{key}{Default} --> {kvp.Value}  [StatusCode 0 = Exception]");
            } else {
                WriteLine($"   {Helper.GetStatusCodeBasedColor(key)}{key}{Default} --> {kvp.Value}");
            }
        }
        NewLine();

        return (parameters.Export, uniqueRequests);
    }

    /// <summary>
    /// Produces a summary for a single result
    /// </summary>
    /// <returns>Value indicating whether export is required, and the requests to export (null if not required)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (bool exportRequired, HashSet<Response> uniqueRequests) SummarizeSingle(Parameters parameters, PulseResult pulseResult) {
        var result = pulseResult.Results.First();
        double duration = result.Latency.TotalMilliseconds;
        var statusCode = result.StatusCode;

        WriteLine($"Request count: {Yellow}1");
        WriteLine($"Total duration: {Yellow}{pulseResult.TotalDuration:hr}");
        if ((int)statusCode is >= 200 and < 300) {
            WriteLine($"Success: {Green}true");
        } else {
            WriteLine($"Success: {Red}false");
        }
        WriteLine($"Latency:      {Green}{duration:0.##}ms");
        WriteLine($"Content Size: {Green}{Utils.Strings.FormatBytes(result.ContentLength)}");
        if (statusCode is 0) {
            WriteLine($"Status code: {Red}0 [Exception]");
        } else {
            WriteLine($"Status code: {Helper.GetStatusCodeBasedColor((int)statusCode)}{statusCode}");
        }
        NewLine();

        var uniqueRequests = new HashSet<Response>(1) { result };

        return (parameters.Export, uniqueRequests);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task ExportUniqueRequestsAsync(Parameters parameters, HashSet<Response> uniqueRequests, CancellationToken token = default) {
        var count = uniqueRequests.Count;

        if (count is 0) {
            WriteLine($"{Yellow}No unique results found to export...");
            return;
        }

        string directory = Path.Join(Directory.GetCurrentDirectory(), parameters.OutputFolder);
        Directory.CreateDirectory(directory);
        Exporter.ClearFiles(directory);

        if (count is 1) {
            await Exporter.ExportResponseAsync(uniqueRequests.First(), directory, parameters, token).ConfigureAwait(false);
            WriteLine($"{Green}1{Default} unique response exported to {Yellow}{directory}");
            return;
        }

        var options = new ParallelOptions {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        };

        await Parallel.ForEachAsync(uniqueRequests, options, async (request, tkn) => await Exporter.ExportResponseAsync(request, directory, parameters, tkn).ConfigureAwait(false)).ConfigureAwait(false);

        WriteLine($"{Green}{count}{Default} unique responses exported to {Yellow}{directory}{Default}");
    }
}