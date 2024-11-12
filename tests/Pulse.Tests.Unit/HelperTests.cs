using System.Net;

using Pulse.Core;

namespace Pulse.Tests.Unit;

public class HelperTests {
    [Theory]
    [InlineData(100, ConsoleColor.Green)]
    [InlineData(80, ConsoleColor.Green)]
    [InlineData(75, ConsoleColor.Green)]
    [InlineData(60, ConsoleColor.Yellow)]
    [InlineData(50, ConsoleColor.Yellow)]
    [InlineData(40, ConsoleColor.Red)]
    [InlineData(0, ConsoleColor.Red)]
    public void Extensions_GetPercentageBasedColor(double percentage, ConsoleColor expected) {
        // Arrange
        var color = Helper.GetPercentageBasedColor(percentage);

        // Assert
        color.ConsoleColor.Should().Be(expected, "because the percentage is correct");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, ConsoleColor.Green)]
    [InlineData((HttpStatusCode)0, ConsoleColor.Magenta)]
    [InlineData(HttpStatusCode.Forbidden, ConsoleColor.Red)]
    [InlineData(HttpStatusCode.BadGateway, ConsoleColor.Red)]
    [InlineData(HttpStatusCode.Ambiguous, ConsoleColor.Yellow)]
    [InlineData(HttpStatusCode.PermanentRedirect, ConsoleColor.Yellow)]
    public void Extensions_GetStatusCodeBasedColor(HttpStatusCode statusCode, ConsoleColor expected) {
        // Arrange
        var color = Helper.GetStatusCodeBasedColor((int)statusCode);

        // Assert
        color.ConsoleColor.Should().Be(expected, "because the percentage is correct");
    }
}