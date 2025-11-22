namespace Pulse.Models;

internal interface IOutputFormatter<T> where T : allows ref struct {
	abstract void OutputAsPlainText();

	abstract void OutputAsJson();
}

internal enum OutputFormat {
	PlainText,
	JSON
}

internal static class OutputFormatterExtensions {
	internal static void Output<T>(this T value, OutputFormat format) where T : IOutputFormatter<T>, allows ref struct {
		switch (format) {
			case OutputFormat.PlainText:
				value.OutputAsPlainText();
				break;
			case OutputFormat.JSON:
				value.OutputAsJson();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(format));
		}
	}
}