using System.Collections.Concurrent;
using System.Net;

using Pulse.Configuration;
using Pulse.Core;
using Pulse.Models;

namespace Pulse.Tests.Unit;

public class SummaryTests {
    [Fact]
    public void Summary_Mean_ReturnsCorrectValue() {
        // Arrange
        var arr = Enumerable.Range(0, 100).Select(_ => Random.Shared.NextDouble()).ToArray();
        var expected = arr.Average();

        // Act
        var actual = PulseSummary.CalculateMean(arr);

        // Assert
        Assert.Equal(expected, actual, 0.01);
    }

    [Theory]
    [ClassData(typeof(SummaryTestData))]
    public void GetSummary_TheoryTests(double[] values, bool removeOutliers, double expectedMin, double expectedMax, double expectedAvg, int expectedRemoved) {
        // Act
        var summary = PulseSummary.GetSummary(values, removeOutliers);

        // Assert
        Assert.Equal(expectedMin, summary.Min, 0.01);
        Assert.Equal(expectedMax, summary.Max, 0.01);
        Assert.Equal(expectedAvg, summary.Mean, 0.01);
        Assert.Equal(expectedRemoved, summary.Removed, 0.01);
    }

    private class SummaryTestData : TheoryData<double[], bool, double, double, double, int> {
        public SummaryTestData() {
            // Test case 1: single element
            Add([42], false, 42, 42, 42, 0);
            // Test case 2: two elements without filtering
            Add([10, 20], false, 10, 20, 15, 0);
            // Test case 3: multiple elements without filtering
            Add([1, 2, 3, 4, 5], false, 1, 5, 3, 0);
            // Test case 4: multiple elements with outliers
            Add([1, 2, 3, 4, 100], true, 1, 4, 2.5, 1);
            // Test case 5: all elements identical without filtering
            Add([5, 5, 5, 5], false, 5, 5, 5, 0);
            // Test case 6: all elements identical with filtering
            Add([5, 5, 5, 5], true, 5, 5, 5, 2);
            // Test case 7: multiple outliers on both ends
            Add([-10.0, 0.0, 1.0, 2.0, 3.0, 100.0], true, 0.0, 3.0, 1.5, 2);
            // Test case 8: large dataset without outliers
            Add(Enumerable.Range(1, 1000).Select(x => (double)x).ToArray(), true, 1.0, 1000.0, 500.5, 0);
            // Test case 9: large dataset with outliers
            Add(Enumerable.Range(1, 1000).Select(x => (double)x).Union([-1000.0, 2000.0]).ToArray(), true, 1.0, 1000.0, 500.5, 2);
        }
    }

    [Fact]
    public async Task Summarize_DeduplicatesResponses_WhenExportEnabled() {
        // Arrange
        var outputFolderName = $"pulse-summary-tests-{Guid.NewGuid():N}";
        var parameters = new Parameters(new ParametersBase {
            Export = true,
            OutputFolder = outputFolderName
        }, TestContext.Current.CancellationToken);
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
            CreateResponse(3, HttpStatusCode.OK, "beta") // Same length as response 2 -> should deduplicate
        };
        var stack = new ConcurrentStack<Response>(responses);
        var pulseResult = new PulseResult {
            Results = stack,
            TotalDuration = TimeSpan.FromSeconds(1),
            SuccessRate = 100
        };

        try {
            // Act
            await PulseSummary.SummarizeAsync(parameters, requestDetails, pulseResult);

            // Assert
            var exportedFiles = Directory.Exists(exportDirectory)
                                ? Directory.GetFiles(exportDirectory)
                                : Array.Empty<string>();

            Assert.Equal(2, exportedFiles.Length);
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
