using PrettyConsole;
using Pulse.Configuration;
using System.Collections.Concurrent;

namespace Pulse.Core;

public static class Extensions {
	/// <summary>
	/// Returns a text color based on percentage
	/// </summary>
	/// <param name="percentage"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static Color GetPercentageBasedColor(double percentage) {
		if ((uint)percentage > 100) {
			throw new ArgumentOutOfRangeException(nameof(percentage), "Must be between 0 and 100");
		}

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
	/// Modifies parameters using args
	/// </summary>
	/// <param name="parameters"></param>
	/// <param name="@base"></param>
	public static void ModifyFromBase(this Parameters parameters, ParametersBase @base) {
		parameters.Requests = @base.Requests;
		parameters.ExecutionMode = @base.ExecutionMode;
		parameters.FormatJson = @base.FormatJson;
		parameters.UseFullEquality = @base.UseFullEquality;
		parameters.Export = @base.Export;
		parameters.NoOp = @base.NoOp;
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