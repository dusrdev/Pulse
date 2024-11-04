using Pulse.Configuration;

namespace Pulse.Tests.Unit;

public class ParametersTests {
    [Fact]
    public void ParametersBase_Default() {
        // Arrange
        var @params = new ParametersBase();

        // Assert
        @params.Requests.Should().Be(1, "because the default is 1");
        @params.ExecutionMode.Should().Be(ExecutionMode.Parallel, "because the default is parallel");
        @params.BatchSize.Should().Be(1, "because the default is 1");
        @params.BatchSizeModified.Should().BeFalse("because the default is false");
        @params.FormatJson.Should().BeFalse("because the default is false");
        @params.UseFullEquality.Should().BeFalse("because the default is false");
        @params.Export.Should().BeTrue("because the default is true");
        @params.NoOp.Should().BeFalse("because the default is false");
        @params.Verbose.Should().BeFalse("because the default is false");
    }

    [Fact]
    public void Parameters_FromBase_KeepsAllValues() {
        // Arrange
        var @params = new Parameters(new ParametersBase(), CancellationToken.None);

        // Assert
        @params.Requests.Should().Be(1, "because the default is 1");
        @params.ExecutionMode.Should().Be(ExecutionMode.Parallel, "because the default is parallel");
        @params.BatchSize.Should().Be(1, "because the default is 1");
        @params.BatchSizeModified.Should().BeFalse("because the default is false");
        @params.FormatJson.Should().BeFalse("because the default is false");
        @params.UseFullEquality.Should().BeFalse("because the default is false");
        @params.Export.Should().BeTrue("because the default is true");
        @params.NoOp.Should().BeFalse("because the default is false");
        @params.Verbose.Should().BeFalse("because the default is false");
    }
}