using System.Collections.Concurrent;
using System.Net;

using Pulse.Core;
using Pulse.Models;

namespace Pulse.Tests;

public class SummaryTests {
    [Test]
    public async Task Summary_Mean_ReturnsCorrectValue() {
        var arr = Enumerable.Range(0, 100).Select(_ => Random.Shared.NextDouble()).ToArray();
        var expected = arr.Average();

        var actual = PulseSummary.CalculateMean(arr);

        await Assert.That(Math.Abs(actual - expected)).IsLessThan(0.01);
    }

    [Test]
    [MethodDataSource(nameof(GetSummaryTestData))]
    public async Task GetSummary_TheoryTests(double[] values, bool removeOutliers, double expectedMin, double expectedMax, double expectedAvg, int expectedRemoved) {
        var summary = PulseSummary.GetSummary(values, removeOutliers);

        await Assert.That(Math.Abs(summary.Min - expectedMin)).IsLessThan(0.01);
        await Assert.That(Math.Abs(summary.Max - expectedMax)).IsLessThan(0.01);
        await Assert.That(Math.Abs(summary.Mean - expectedAvg)).IsLessThan(0.01);
        await Assert.That(summary.Removed).IsEqualTo(expectedRemoved);
    }

    public static IEnumerable<Func<(double[] values, bool removeOutliers, double expectedMin, double expectedMax, double expectedAvg, int expectedRemoved)>> GetSummaryTestData() {
        yield return () => (new[] { 42d }, false, 42d, 42d, 42d, 0);
        yield return () => (new[] { 10d, 20d }, false, 10d, 20d, 15d, 0);
        yield return () => (new[] { 1d, 2d, 3d, 4d, 5d }, false, 1d, 5d, 3d, 0);
        yield return () => (new[] { 1d, 2d, 3d, 4d, 100d }, true, 1d, 4d, 2.5d, 1);
        yield return () => (new[] { 5d, 5d, 5d, 5d }, false, 5d, 5d, 5d, 0);
        yield return () => (new[] { 5d, 5d, 5d, 5d }, true, 5d, 5d, 5d, 2);
        yield return () => (new[] { -10d, 0d, 1d, 2d, 3d, 100d }, true, 0d, 3d, 1.5d, 2);
        yield return () => (Enumerable.Range(1, 1000).Select(x => (double)x).ToArray(), true, 1d, 1000d, 500.5d, 0);
        yield return () => (Enumerable.Range(1, 1000).Select(x => (double)x).Union([-1000d, 2000d]).ToArray(), true, 1d, 1000d, 500.5d, 2);
    }

    [Test]
    public async Task Summarize_DeduplicatesResponses_WhenExportEnabled() {
        var outputFolderName = $"pulse-summary-tests-{Guid.NewGuid():N}";
        var parameters = new Parameters(new ParametersBase {
            Export = true,
            OutputFolder = outputFolderName,
            Quiet = true
        }, CancellationToken.None);
        var exportDirectory = Path.Join(Directory.GetCurrentDirectory(), outputFolderName);
        var requestDetails = new RequestDetails {
            Request = new Request {
                Url = "https://example.com",
                Method = HttpMethod.Get
            }
        };
        var responses = new[] {
            CreateResponse(1, HttpStatusCode.OK, "alpha"),
            CreateResponse(2, HttpStatusCode.OK, "beta"),
            CreateResponse(3, HttpStatusCode.OK, "beta")
        };
        var stack = new ConcurrentStack<Response>(responses);
        var pulseResult = new PulseResult {
            Results = stack,
            TotalDuration = TimeSpan.FromSeconds(1),
            SuccessRate = 100
        };

        try {
            await PulseSummary.SummarizeAsync(parameters, requestDetails, pulseResult);

            var exportedFiles = Directory.Exists(exportDirectory)
                                ? Directory.GetFiles(exportDirectory)
                                : Array.Empty<string>();

            await Assert.That(exportedFiles.Length).IsEqualTo(2);
        } finally {
            if (Directory.Exists(exportDirectory)) {
                Directory.Delete(exportDirectory, recursive: true);
            }
        }
    }

    private static Response CreateResponse(int id, HttpStatusCode statusCode, string content) {
        return new Response {
            Id = id,
            StatusCode = statusCode,
            Headers = Array.Empty<KeyValuePair<string, IEnumerable<string>>>(),
            Content = content,
            ContentLength = content.Length,
            Latency = TimeSpan.FromMilliseconds(10),
            Exception = StrippedException.Default,
            CurrentConcurrentConnections = 1
        };
    }
}