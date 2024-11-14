using static PrettyConsole.Console;
using PrettyConsole;

using Pulse.Configuration;


namespace Pulse.Core;

/// <summary>
/// Helper class
/// </summary>
public static class Helper {
    /// <summary>
    /// Returns a text color based on percentage
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Color GetPercentageBasedColor(double percentage) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)percentage, 100);

        return percentage switch {
            >= 75 => Color.Green,
            >= 50 => Color.Yellow,
            _ => Color.Red
        };
    }

    /// <summary>
    /// Returns a text color based on http status code
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public static Color GetStatusCodeBasedColor(int statusCode) {
        return statusCode switch {
            < 100 => Color.Magenta,
            < 200 => Color.White,
            < 300 => Color.Green,
            < 400 => Color.Yellow,
            < 600 => Color.Red,
            _ => Color.Magenta
        };
    }

    /// <summary>
    /// Returns a colored header for the request
    /// </summary>
    /// <param name="request"></param>
    public static ColoredOutput[] CreateHeader(Request request) {
        Color color = request.Method.Method switch {
            "GET" => Color.Green,
            "DELETE" => Color.Red,
            "POST" => Color.Magenta,
            _ => Color.Yellow
        };

        return [request.Method.Method * color, " => ", request.Url];
    }

    /// <summary>
    /// Configures SSL handling
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="proxy"></param>
    public static void ConfigureSslHandling(this SocketsHttpHandler handler, Proxy proxy) {
        if (proxy.IgnoreSSL) {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }
    }

    /// <summary>
    /// Prints the exception
    /// </summary>
    /// <param name="e"></param>
    public static void PrintException(this StrippedException e) {
        WriteLine(["Exception Type: " * Color.Yellow, e.Type], OutputPipe.Error);
        WriteLine(["Message: " * Color.Yellow, e.Message], OutputPipe.Error);
    }
}