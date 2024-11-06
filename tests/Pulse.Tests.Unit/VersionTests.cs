namespace Pulse.Tests.Unit;

public class VersionTests {
    [Fact]
    public void Assembly_Version_Matching() {
        // Arrange
        var constantVersion = Version.Parse(Program.VERSION);
        var assemblyVersion = typeof(Program).Assembly.GetName().Version!;

        // Assert
        constantVersion.Should().Be(assemblyVersion);
    }
}