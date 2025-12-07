namespace Pulse.Models;

internal interface IOutputFormatter {
    void OutputAsPlainText();

    void OutputAsJson();
}

internal enum OutputFormat {
    PlainText,
    JSON
}

internal static class OutputFormatterExtensions {
    internal static void Print(this IOutputFormatter value, OutputFormat format) {
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