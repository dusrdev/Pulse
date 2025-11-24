using Pulse.Models;

namespace Pulse.Tests;

public class StrippedExceptionTests {
    [Test]
    public async Task StrippedException_Default_AllEmpty() {
        var exception = StrippedException.Default;

        await Assert.That(exception.IsDefault).IsTrue();
        await Assert.That(exception.Type).IsEqualTo(string.Empty);
        await Assert.That(exception.Message).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task StrippedException_JsonCtor_YieldsDefault() {
        var exception = new StrippedException();

        await Assert.That(exception.IsDefault).IsTrue();
        await Assert.That(exception.Type).IsEqualTo(string.Empty);
        await Assert.That(exception.Message).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task StrippedException_FromException_NullYieldsDefault() {
        var exception = StrippedException.FromException(null);

        await Assert.That(exception.IsDefault).IsTrue();
        await Assert.That(exception.Type).IsEqualTo(string.Empty);
        await Assert.That(exception.Message).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task StrippedException_FromException_ExceptionYieldsTypeAndMessage() {
        var exception = new Exception("Test");

        var stripped = StrippedException.FromException(exception);

        await Assert.That(stripped.IsDefault).IsFalse();
        await Assert.That(stripped.Type).IsEqualTo(exception.GetType().Name);
        await Assert.That(stripped.Message).IsEqualTo(exception.Message);
    }
}
