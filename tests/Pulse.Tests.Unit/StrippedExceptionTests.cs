using Pulse.Configuration;

namespace Pulse.Tests.Unit;

public class StrippedExceptionTests {
    [Fact]
    public void StrippedException_Default_AllEmpty() {
        // Arrange
        var exception = StrippedException.Default;

        // Assert
        exception.IsDefault.Should().BeTrue("because the exception is the default");
        exception.Type.Should().BeEmpty("because the exception is the default");
        exception.Message.Should().BeEmpty("because the exception is the default");
    }

    [Fact]
    public void StrippedException_JsonCtor_YieldsDefault() {
        // Arrange
        var exception = new StrippedException();

        // Assert
        exception.IsDefault.Should().BeTrue("because the exception is the default");
        exception.Type.Should().BeEmpty("because the exception is the default");
        exception.Message.Should().BeEmpty("because the exception is the default");
    }

    [Fact]
    public void StrippedException_FromException_NullYieldsDefault() {
        // Arrange
        var exception = StrippedException.FromException(null);

        // Assert
        exception.IsDefault.Should().BeTrue("because the exception is the default");
        exception.Type.Should().BeEmpty("because the exception is the default");
        exception.Message.Should().BeEmpty("because the exception is the default");
    }

    [Fact]
    public void StrippedException_FromException_ExceptionYieldsTypeAndMessage() {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var stripped = StrippedException.FromException(exception);

        // Assert
        stripped.IsDefault.Should().BeFalse("because the exception is not the default");
        stripped.Type.Should().Be(exception.GetType().Name, "because the exception type is correct");
        stripped.Message.Should().Be(exception.Message, "because the exception message is correct");
    }
}