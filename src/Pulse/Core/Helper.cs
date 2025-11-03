using System.Net;
using System.Runtime.CompilerServices;

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
    public static Color GetStatusCodeBasedColor(int statusCode) {
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetMethodBasedColor(string method)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConfigureSslHandling(this SocketsHttpHandler handler, Proxy proxy) {
        if (proxy.IgnoreSSL) {
            handler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
        }
    }

    /// <summary>
    /// Prints the exception
    /// </summary>
    /// <param name="e"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintException(this StrippedException e) {
        WriteLine(OutputPipe.Error, $"{Yellow}Exception type: {Default}{e.Type}");
        WriteLine(OutputPipe.Error, $"{Yellow}Message: {Default}{e.Message}");

        if (e.Detail is not null) {
            WriteLine(OutputPipe.Error, $"{Yellow}Detail: {Default}{e.Detail}");
        }

        if (e.InnerException is null or { IsDefault: true }) {
            return;
        }

        NewLine(OutputPipe.Error);
        WriteLine($"{Magenta}Inner exception:");
        PrintException(e.InnerException);
    }

    /// <summary>
    /// Returns an exception detail if any
    /// </summary>
    /// <param name="details"></param>
    /// <param name="exception"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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