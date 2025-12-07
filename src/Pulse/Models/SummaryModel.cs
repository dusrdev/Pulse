using System.Net;
using System.Text.Json;

using Pulse.Core;

namespace Pulse.Models;

internal readonly struct SummaryModel : IOutputFormatter {
    public required Target Target { get; init; }
    public required int RequestCount { get; init; }
    public required int ConcurrentConnections { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required double SuccessRate { get; init; }
    public required MinMeanMax LatencyInMilliseconds { get; init; }
    public required int LatencyOutliersRemoved { get; init; }
    public required MinMeanMax ContentSizeInBytes { get; init; }
    public required double ThroughputBytesPerSecond { get; init; }
    public required Dictionary<HttpStatusCode, int> StatusCodeCounts { get; init; }

    public void OutputAsJson() {
        JsonSerializer.ToConsoleOut(in this, ModelsJsonContext.Default.SummaryModel);
    }

    public void OutputAsPlainText() {
        Console.WriteLineInterpolated($"{Helper.GetMethodBasedColor(Target.HttpMethod)}{Target.HttpMethod}{ConsoleColor.DefaultForeground} => {Markup.Underline}{Target.Url}{Markup.ResetUnderline}");
        Console.WriteLineInterpolated($"Request count: {Yellow}{RequestCount}");
        Console.WriteLineInterpolated($"Concurrent connections: {Yellow}{ConcurrentConnections}");
        Console.WriteLineInterpolated($"Total duration: {Yellow}{TotalDuration:duration}");
        Console.WriteLineInterpolated($"Success Rate: {Helper.GetPercentageBasedColor(SuccessRate)}{SuccessRate}%");
        Console.WriteLineInterpolated($"Latency:       Min: {Green}{LatencyInMilliseconds.Min:0.##}ms{ConsoleColor.DefaultForeground}, Mean: {Yellow}{LatencyInMilliseconds.Mean:0.##}ms{ConsoleColor.DefaultForeground}, Max: {Red}{LatencyInMilliseconds.Max:0.##}ms");
        if (LatencyOutliersRemoved != 0) {
            Console.WriteLineInterpolated($"               (Removed {DarkYellow}{LatencyOutliersRemoved}{ConsoleColor.DefaultForeground} {(LatencyOutliersRemoved == 1 ? "outlier" : "outliers")})");
        }
        Console.WriteLineInterpolated($"Content Size:  Min: {Green}{ContentSizeInBytes.Min:bytes}{ConsoleColor.DefaultForeground}, Mean: {Yellow}{ContentSizeInBytes.Mean:bytes}{ConsoleColor.DefaultForeground}, Max: {Red}{ContentSizeInBytes.Max:bytes}");
        Console.WriteLineInterpolated($"Total throughput: {Yellow}{ThroughputBytesPerSecond:bytes}/s");
        Console.WriteLineInterpolated($"Status codes:");
        foreach (var kvp in StatusCodeCounts.OrderBy(static s => (int)s.Key)) {
            var key = (int)kvp.Key;
            if (key is 0) {
                Console.WriteLineInterpolated($"   {Magenta}{key}{ConsoleColor.DefaultForeground} --> {kvp.Value}  [StatusCode 0 = Exception]");
            } else {
                Console.WriteLineInterpolated($"   {Helper.GetStatusCodeBasedColor(key)}{key}{ConsoleColor.DefaultForeground} --> {kvp.Value}");
            }
        }
        Console.NewLine();
    }
}