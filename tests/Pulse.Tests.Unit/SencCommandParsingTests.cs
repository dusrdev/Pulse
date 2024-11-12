using Pulse.Core;

using Sharpify.CommandLineInterface;

namespace Pulse.Tests.Unit;

public class SendCommandParsingTests {
    [Fact]
    public void Arguments_Flag_NoOp() {
        // Arrange
        var args = Parser.ParseArguments("pulse --noop")!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.NoOp.Should().BeTrue("because the flag is present");
    }

    [Theory]
    [InlineData("Pulse -v", -1)] // default
    [InlineData("Pulse -v -t -1", -1)] // set but default
    [InlineData("Pulse --verbose -t 30000", 30000)] // custom
    [InlineData("Pulse --verbose --timeout 30000", 30000)] // custom
    public void Arguments_Timeout(string arguments, int expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.TimeoutInMs.Should().Be(expected, "because parsed or default");
    }

    [Theory]
    [InlineData("Pulse --delay 50", 0)] // default
    [InlineData("Pulse --delay -9", 0)] // default
    [InlineData("Pulse -m Sequential", 0)] // not set but default
    [InlineData("Pulse -m Sequential --delay 50", 50)] // set
    [InlineData("Pulse -m Sequential --delay -9", 0)] // set but default since negative
    public void Arguments_Delay(string arguments, int expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.DelayInMs.Should().Be(expected, "because parsed or default");
    }

    [Theory]
    [InlineData("Pulse -v")]
    [InlineData("Pulse --verbose")]
    public void Arguments_Flag_Verbose(string arguments) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.Verbose.Should().BeTrue("because the flag is present");
    }

    [Theory]
    [InlineData("Pulse -v")]
    [InlineData("Pulse --verbose")]
    public void Arguments_BatchSize_NotModified(string arguments) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.MaxConnectionsModified.Should().BeFalse("because the option is not present");
        @params.MaxConnections.Should().Be(1, "because the default is 1");
    }

    [Fact]
    public void Arguments_MaxConnections_Modified() {
        // Arrange
        var args = Parser.ParseArguments("Pulse -c 5")!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.MaxConnectionsModified.Should().BeTrue("because the option is present");
        @params.MaxConnections.Should().Be(5, "because the 5 is requested");
    }

    [Theory]
    [InlineData("Pulse -m sequential")]
    [InlineData("Pulse --mode sequential")]
    public void Arguments_ModeSequential(string arguments) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.ExecutionMode.Should().Be(Configuration.ExecutionMode.Sequential, "because the option is requested");
    }

    [Theory]
    [InlineData("Pulse -m parallel")]
    [InlineData("Pulse --mode parallel")]
    [InlineData("Pulse")]
    public void Arguments_ModeParallel(string arguments) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.ExecutionMode.Should().Be(Configuration.ExecutionMode.Parallel, "because the option is requested or default");
    }

    [Theory]
    [InlineData("Pulse", 1)]
    [InlineData("Pulse -n 1", 1)]
    [InlineData("Pulse --number 1", 1)]
    [InlineData("Pulse -n 0", 1)] // Zero is not allowed - default is used
    [InlineData("Pulse -n -1", 1)] // Negative number is not allowed - default is used
    [InlineData("Pulse -n 50", 50)]
    [InlineData("Pulse --number 50", 50)]
    public void Arguments_NumberOfRequests(string arguments, int expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.Requests.Should().Be(expected, "because the option is requested or default");
    }

    [Theory]
    [InlineData("Pulse", true)]
    [InlineData("Pulse --no-export", false)]
    public void Arguments_NoExport(string arguments, bool expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.Export.Should().Be(expected, "because the option is requested or default");
    }

    [Theory]
    [InlineData("Pulse", false)]
    [InlineData("Pulse --json", true)]
    public void Arguments_FormatJson(string arguments, bool expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.FormatJson.Should().Be(expected, "because the option is requested or default");
    }

    [Theory]
    [InlineData("Pulse", false)]
    [InlineData("Pulse -f", true)]
    public void Arguments_UseFullEquality(string arguments, bool expected) {
        // Arrange
        var args = Parser.ParseArguments(arguments)!;

        // Act
        var @params = SendCommand.ParseParametersArgs(args);

        // Assert
        @params.UseFullEquality.Should().Be(expected, "because the option is requested or default");
    }
}