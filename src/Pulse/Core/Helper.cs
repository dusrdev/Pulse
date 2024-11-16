using static PrettyConsole.Console;
using PrettyConsole;

using Pulse.Configuration;
using System.Net;


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
    public static void PrintException(this StrippedException e, int indent = 0) {
        Span<char> padding = stackalloc char[indent];
        padding.Fill(' ');
        Error.Write(padding);
        WriteLine(["Exception Type" * Color.Yellow, ": ", e.Type], OutputPipe.Error);
        Error.Write(padding);
        WriteLine(["Message" * Color.Yellow, ": ", e.Message], OutputPipe.Error);
        if (e.Detail is not null) {
            Error.Write(padding);
            WriteLine(["Detail" * Color.Yellow, ": ", e.Detail], OutputPipe.Error);
        }
        if (e.InnerException is null or { IsDefault: true }) {
            return;
        }
        Error.Write(padding);
        Error.WriteLine("Inner Exception:");
        PrintException(e.InnerException, indent + 2);
    }

    /// <summary>
    /// Returns an exception detail if any
    /// </summary>
    /// <param name="details"></param>
    /// <param name="exception"></param>
    public static string? AddExceptionDetail(Exception exception) {
        switch (exception) {
            case HttpRequestException: {
                    var e = exception as HttpRequestException;
                    return $"HttpRequestError: {e!.HttpRequestError}";
                }
            case WebException: {
                    var e = exception as WebException;
                    return $"WebExceptionStatus: {e!.Status}";
                }
        }
        return null;
    }
}