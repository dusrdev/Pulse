using System.Net;
using System.Numerics;

using Pulse.Configuration;
using Pulse.Models;

namespace Pulse.Core;

/// <summary>
/// Helper class
/// </summary>
internal static class Helper {
    public static double Percentage<T>(T current, T total) where T : INumberBase<T> {
        return double.CreateChecked(current / total);
    }

    public static TimeSpan GetETA(double percentage, TimeSpan elapsed) {
        if (percentage <= 0) return TimeSpan.MaxValue;
        if (percentage >= 1) return TimeSpan.Zero;
        var rem = (1 - percentage) / percentage;
        return rem * elapsed;
    }

    // Returns an OutputFormat based on the llm parameter
    public static OutputFormat OutputFormatFromBool(bool llm = false)
        => llm ? OutputFormat.JSON : OutputFormat.PlainText;

    /// <summary>
    /// Returns a text color based on percentage
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ConsoleColor GetPercentageBasedColor(double percentage) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)percentage, 100);

        return percentage switch {
            >= 75 => Green,
            >= 50 => Yellow,
            _ => Red
        };
    }

    /// <summary>
    /// Returns a text color based on http status code
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public static ConsoleColor GetStatusCodeBasedColor(int statusCode) {
        return statusCode switch {
            < 100 => Magenta,
            < 200 => White,
            < 300 => Green,
            < 400 => Yellow,
            < 600 => Red,
            _ => Magenta
        };
    }

    /// <summary>
	/// Returns a color based on HttpMethod
	/// </summary>
	/// <param name="method"></param>
	/// <returns></returns>
    public static ConsoleColor GetMethodBasedColor(string method)
        => method switch {
            "GET" => Green,
            "DELETE" => Red,
            "POST" => Magenta,
            _ => Yellow
        };

    /// <summary>
    /// Configures SSL handling
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="proxy"></param>
    public static void ConfigureSslHandling(this SocketsHttpHandler handler, Proxy proxy) {
        if (proxy.IgnoreSSL) {
#pragma warning disable CA5359 // Do Not Disable Certificate Validation
            handler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
#pragma warning restore CA5359 // Do Not Disable Certificate Validation
        }
    }

    /// <summary>
    /// Prints the exception
    /// </summary>
    /// <param name="e"></param>
    public static void PrintException(this StrippedException e) {
        Console.WriteLineInterpolated(OutputPipe.Error, $"{Yellow}Exception type: {ConsoleColor.Default}{e.Type}");
        Console.WriteLineInterpolated(OutputPipe.Error, $"{Yellow}Message: {ConsoleColor.Default}{e.Message}");

        if (e.Detail is not null) {
            Console.WriteLineInterpolated(OutputPipe.Error, $"{Yellow}Detail: {ConsoleColor.Default}{e.Detail}");
        }

        if (e.InnerException is null or { IsDefault: true }) {
            return;
        }

        Console.NewLine(OutputPipe.Error);
        Console.WriteLineInterpolated(OutputPipe.Error, $"{Magenta}Inner exception:");
        PrintException(e.InnerException);
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