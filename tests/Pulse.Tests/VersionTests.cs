using Pulse.Core;

namespace Pulse.Tests;

public class VersionTests {
    [Test]
    public async Task Assembly_Version_Matching() {
        var constantVersion = Version.Parse(Commands.Version);
        var assemblyVersion = typeof(Program).Assembly.GetName().Version!;

        await Assert.That(constantVersion).IsEqualTo(assemblyVersion);
    }
}