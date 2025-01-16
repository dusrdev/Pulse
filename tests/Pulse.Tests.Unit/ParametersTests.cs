using Pulse.Configuration;

namespace Pulse.Tests.Unit;

public class ParametersTests {
    [Fact]
    public void ParametersBase_Default() {
        // Arrange
        var @params = new ParametersBase();

        // Assert
        Assert.Equal(1, @params.Requests);
        Assert.Equal(ExecutionMode.Parallel, @params.ExecutionMode);
        Assert.Equal(1, @params.MaxConnections);
        Assert.False(@params.MaxConnectionsModified);
        Assert.False(@params.FormatJson);
        Assert.False(@params.UseFullEquality);
        Assert.True(@params.Export);
        Assert.False(@params.NoOp);
        Assert.False(@params.Verbose);
    }

    [Fact]
    public void Parameters_FromBase_KeepsAllValues() {
        // Arrange
        var @params = new Parameters(new ParametersBase(), CancellationToken.None);

        // Assert
        Assert.Equal(1, @params.Requests);
        Assert.Equal(ExecutionMode.Parallel, @params.ExecutionMode);
        Assert.Equal(1, @params.MaxConnections);
        Assert.False(@params.MaxConnectionsModified);
        Assert.False(@params.FormatJson);
        Assert.False(@params.UseFullEquality);
        Assert.True(@params.Export);
        Assert.False(@params.NoOp);
        Assert.False(@params.Verbose);
    }
}