using Pulse.Configuration;

namespace Pulse.Tests.Unit;

public class StrippedExceptionTests {
    [Fact]
    public void StrippedException_Default_AllEmpty() {
        // Arrange
        var exception = StrippedException.Default;

        // Assert
        Assert.True(exception.IsDefault);
        Assert.Empty(exception.Type);
        Assert.Empty(exception.Message);
    }

    [Fact]
    public void StrippedException_JsonCtor_YieldsDefault() {
        // Arrange
        var exception = new StrippedException();

        // Assert
        Assert.True(exception.IsDefault);
        Assert.Empty(exception.Type);
        Assert.Empty(exception.Message);
    }

    [Fact]
    public void StrippedException_FromException_NullYieldsDefault() {
        // Arrange
        var exception = StrippedException.FromException(null);

        // Assert
        Assert.True(exception.IsDefault);
        Assert.Empty(exception.Type);
        Assert.Empty(exception.Message);
    }

    [Fact]
    public void StrippedException_FromException_ExceptionYieldsTypeAndMessage() {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var stripped = StrippedException.FromException(exception);

        // Assert
        Assert.False(stripped.IsDefault);
        Assert.Equal(exception.GetType().Name, stripped.Type);
        Assert.Equal(exception.Message, stripped.Message);
    }
}