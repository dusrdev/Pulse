using System.Net;

using Pulse.Core;

namespace Pulse.Tests;

public class HelperTests {
    [Test]
    [Arguments(100, ConsoleColor.Green)]
    [Arguments(80, ConsoleColor.Green)]
    [Arguments(75, ConsoleColor.Green)]
    [Arguments(60, ConsoleColor.Yellow)]
    [Arguments(50, ConsoleColor.Yellow)]
    [Arguments(40, ConsoleColor.Red)]
    [Arguments(0, ConsoleColor.Red)]
    public async Task Extensions_GetPercentageBasedColor(double percentage, ConsoleColor expected) {
        var color = Helper.GetPercentageBasedColor(percentage);

        await Assert.That(color).IsEqualTo(expected);
    }

    [Test]
    [Arguments(HttpStatusCode.OK, ConsoleColor.Green)]
    [Arguments((HttpStatusCode)0, ConsoleColor.Magenta)]
    [Arguments(HttpStatusCode.Forbidden, ConsoleColor.Red)]
    [Arguments(HttpStatusCode.BadGateway, ConsoleColor.Red)]
    [Arguments(HttpStatusCode.Ambiguous, ConsoleColor.Yellow)]
    [Arguments(HttpStatusCode.PermanentRedirect, ConsoleColor.Yellow)]
    public async Task Extensions_GetStatusCodeBasedColor(HttpStatusCode statusCode, ConsoleColor expected) {
        var color = Helper.GetStatusCodeBasedColor((int)statusCode);

        await Assert.That(color).IsEqualTo(expected);
    }
}