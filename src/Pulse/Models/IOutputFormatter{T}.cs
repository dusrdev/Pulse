namespace Pulse.Models;

internal interface IOutputFormatter<T> where T : allows ref struct {
	void Output(OutputFormat format) {
		switch (format) {
			case OutputFormat.PlainText:
				OutputAsPlainText();
				break;
			case OutputFormat.JSON:
				OutputAsJson();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(format));
		}
	}

	abstract void OutputAsPlainText();

	abstract void OutputAsJson();
}

internal enum OutputFormat {
	PlainText,
	JSON
}