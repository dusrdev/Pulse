using PrettyConsole;

namespace Pulse.Core;

/// <summary>
/// Extensions
/// </summary>
public static class Extensions {
	/// <summary>
	/// Returns a text color based on percentage
	/// </summary>
	/// <param name="percentage"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static Color GetPercentageBasedColor(double percentage) {
		ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)percentage, 100);

		return percentage switch {
			> 75 => Color.Green,
			> 50 => Color.Yellow,
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
	/// Configures SSL handling
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="proxy"></param>
	public static void ConfigureSSLHandling(this SocketsHttpHandler handler, Proxy proxy) {
		if (proxy.IgnoreSSL) {
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
		}
	}
}