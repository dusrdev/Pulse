using PrettyConsole;
using Pulse.Configuration;
using Sharpify.CommandLineInterface;
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
		if (percentage is < 0 or > 100) {
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
	/// <param name="args"></param>
	public static void ModifyFromArgs(this Parameters parameters, Arguments args) {
		args.TryGetValue(["n", "number"], 100, out int n);
		parameters.Requests = n;
		args.TryGetEnum(["c", "concurrency"], ConcurrencyMode.Maximum, true, out var concurrencyMode);
		parameters.ConcurrencyMode = concurrencyMode;
		args.TryGetValue("b", 1, out int concurrentRequests); //TODO: bounded - rename
		if (concurrencyMode is not ConcurrencyMode.Limited) {
			concurrentRequests = 1;
		}
		parameters.ConcurrentRequests = concurrentRequests;
		parameters.UseResilience = args.HasFlag("r") || args.HasFlag("resilient");
		parameters.NoExport = args.HasFlag("no-export");
		parameters.UseFullEquality = args.HasFlag("e");
	}

	/// <summary>
	/// Modifies parameters using args
	/// </summary>
	/// <param name="parameters"></param>
	/// <param name="@base"></param>
	public static void ModifyFromBase(this Parameters parameters, ParametersBase @base) {
		parameters.Requests = @base.Requests;
		parameters.ConcurrencyMode = @base.ConcurrencyMode;
		parameters.ConcurrentRequests = @base.ConcurrentRequests;
		parameters.UseResilience = @base.UseResilience;
		parameters.UseFullEquality = @base.UseFullEquality;
		parameters.NoExport = @base.NoExport;
	}

	/// <summary>
	/// Creates a stack of messages
	/// </summary>
	/// <param name="request"></param>
	/// <param name="count"></param>
	/// <returns></returns>
	public static ConcurrentStack<HttpRequestMessage> CreateMessages(this Request request, int count) {
		ConcurrentStack<HttpRequestMessage> messages = new();

		while (count-- > 0) { // Optimized for Arm64 Branch-Decrement-Equal-0
			messages.Push(request.CreateMessage());
		}

		return messages;
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