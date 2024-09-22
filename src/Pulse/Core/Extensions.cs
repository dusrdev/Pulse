using System.Buffers;

using static PrettyConsole.Console;
using PrettyConsole;
using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using System.Collections.Concurrent;

namespace Pulse.Core;

public static class Extensions {
	/// <summary>
	/// Clones an http request message
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request) {
		var clone = new HttpRequestMessage(request.Method, request.RequestUri);

		// Clone the request content
		if (request.Content != null) {
			var memoryStream = new MemoryStream();
			await request.Content.CopyToAsync(memoryStream);
			memoryStream.Position = 0;
			clone.Content = new StreamContent(memoryStream);

			// Copy the content headers
			foreach (var header in request.Content.Headers) {
				clone.Content.Headers.Add(header.Key, header.Value);
			}
		}

		// Copy the headers
		foreach (var header in request.Headers) {
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		// Copy the properties
		foreach (var property in request.Options) {
			clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
		}

		clone.Version = request.Version;

		return clone;
	}

	/// <summary>
	/// Overrides 2 current lines for progress
	/// </summary>
	/// <param name="output1"></param>
	/// <param name="output2"></param>
	public static void OverrideCurrent2Lines(ReadOnlySpan<ColoredOutput> output1, ReadOnlySpan<ColoredOutput> output2) {
        using var memoryOwner = MemoryPool<char>.Shared.Rent(System.Console.BufferWidth);
        Span<char> emptyLine = memoryOwner.Memory.Span.Slice(0, System.Console.BufferWidth);
        emptyLine.Fill(' ');
        var currentLine = System.Console.CursorTop;
        System.Console.SetCursorPosition(0, currentLine);
        System.Console.Error.WriteLine(emptyLine);
        System.Console.Error.WriteLine(emptyLine);
        System.Console.SetCursorPosition(0, currentLine);
        WriteLine(output1);
        WriteLine(output2);
        System.Console.SetCursorPosition(0, currentLine);
    }

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
		args.TryGetValue("n", 100, out int n);
		parameters.Requests = n;
		args.TryGetEnum("c", ConcurrencyMode.Maximum, true, out var concurrencyMode);
		parameters.ConcurrencyMode = concurrencyMode;
		args.TryGetValue("b", 1, out int concurrentRequests);
		if (concurrencyMode is not ConcurrencyMode.Limited) {
			concurrentRequests = 1;
		}
		parameters.ConcurrentRequests = concurrentRequests;
		parameters.UseResilience = args.HasFlag("r");
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

	public static ConcurrentStack<HttpRequestMessage> CreateMessages(this Request request, int count) {
		ConcurrentStack<HttpRequestMessage> messages = new();

		while (count-- > 0) { // Optimized for Arm64 Branch-Decrement-Equal-0
			messages.Push(request.CreateMessage());
		}

		return messages;
	}
}