using Pulse.Configuration;

namespace Pulse.Tests.Unit;

public class ParametersTests {
    [Fact]
    public void ParametersBase_Default() {
        // Arrange
        var @params = new ParametersBase();

        // Assert
        Assert.Equal(1, @params.Requests);
        Assert.Equal(1, @params.Connections);
        Assert.False(@params.FormatJson);
        Assert.False(@params.UseFullEquality);
        Assert.True(@params.Export);
        Assert.False(@params.NoOp);
        Assert.False(@params.Verbose);
    }
}