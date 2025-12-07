using Pulse.Models;

namespace Pulse.Tests;

public class ParametersTests {
    [Test]
    public async Task ParametersBase_Default() {
        var @params = new ParametersBase();

        await Assert.That(@params.Requests).IsEqualTo(1);
        await Assert.That(@params.Connections).IsEqualTo(1);
        await Assert.That(@params.FormatJson).IsFalse();
        await Assert.That(@params.UseFullEquality).IsFalse();
        await Assert.That(@params.Export).IsTrue();
        await Assert.That(@params.NoOp).IsFalse();
    }
}
