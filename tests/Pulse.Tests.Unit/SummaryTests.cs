using Pulse.Core;

namespace Pulse.Tests.Unit;

public class SummaryTests {
    [Fact]
    public void Summary_Mean_ReturnsCorrectValue() {
        // Arrange
        var arr = Enumerable.Range(0, 100).Select(_ => Random.Shared.NextDouble()).ToArray();
        var expected = arr.Average();

        // Act
        var actual = PulseSummary.Mean(arr);

        // Assert
        actual.Should().BeApproximately(expected, 0.01, "because the sum is correct");
    }

    [Theory]
    [ClassData(typeof(SummaryTestData))]
    public void GetSummary_TheoryTests(double[] values, bool removeOutliers, double expectedMin, double expectedMax, double expectedAvg, int expectedRemoved) {
        // Act
        var summary = PulseSummary.GetSummary(values, removeOutliers);

        // Assert
        summary.Min.Should().BeApproximately(expectedMin, 0.01, "because the min is correct");
        summary.Max.Should().BeApproximately(expectedMax, 0.01, "because the max is correct");
        summary.Mean.Should().BeApproximately(expectedAvg, 0.01, "because the avg is correct");
        summary.Removed.Should().Be(expectedRemoved, "because the removed count is correct");
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
}