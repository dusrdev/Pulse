using System.Buffers;

using static PrettyConsole.Console;
using PrettyConsole;
using System.Net;

namespace Pulse.Core;

public static class Extensions {
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
}