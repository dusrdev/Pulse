using System.Text.Json;

namespace Pulse.Models;

internal readonly struct RunConfiguration : IOutputFormatter {
	public required Parameters Parameters { get; init; }
	public required RequestDetails RequestDetails { get; init; }

    public void OutputAsJson() {
		JsonSerializer.ToConsoleOut(in this, ModelsJsonContext.Default.RunConfiguration);
    }

    public void OutputAsPlainText() {
		ConsoleColor headerColor = Cyan;
        ConsoleColor property = DarkGray;
        ConsoleColor value = White;

        // Options
        Console.WriteLineInterpolated($"{headerColor}Options:");
        Console.WriteLineInterpolated($"{property}  Request count: {value}{Parameters.Requests}");
        Console.WriteLineInterpolated($"{property}  Concurrent connections: {value}{Parameters.Connections}");
        Console.WriteLineInterpolated($"{property}  Delay: {value}{Parameters.DelayInMs}ms");
        Console.WriteLineInterpolated($"{property}  Timeout: {value}{Parameters.TimeoutInMs}");
        Console.WriteLineInterpolated($"{property}  Export Raw: {value}{Parameters.ExportRaw}");
        Console.WriteLineInterpolated($"{property}  Format JSON: {value}{Parameters.FormatJson}");
        Console.WriteLineInterpolated($"{property}  Export Full Equality: {value}{Parameters.UseFullEquality}");
        Console.WriteLineInterpolated($"{property}  Export: {value}{Parameters.Export}");
		Console.WriteLineInterpolated($"{property}  Verbose: {value}{Parameters.Verbose}");
		Console.WriteLineInterpolated($"{property}  OutputFormat: {value}{Parameters.OutputFormat}");
		Console.WriteLineInterpolated($"{property}  Quiet: {value}{Parameters.Quiet}");
        Console.WriteLineInterpolated($"{property}  Output Folder: {value}{Parameters.OutputFolder}");

        // Request
        Console.WriteLineInterpolated($"{headerColor}Request:");
        Console.WriteLineInterpolated($"{property}  URL: {value}{RequestDetails.Request.Url}");
        Console.WriteLineInterpolated($"{property}  Method: {value}{RequestDetails.Request.Method}");
        Console.WriteLineInterpolated($"{Yellow}  Headers:");
        if (RequestDetails.Request.Headers.Count > 0) {
            foreach (var header in RequestDetails.Request.Headers) {
                if (header.Value is null) {
                    continue;
                }
                Console.WriteLineInterpolated($"{property}    {header.Key}: {value}{header.Value.Value}");
            }
        }
        if (RequestDetails.Request.Content.Body.HasValue) {
            Console.WriteLineInterpolated($"{Yellow}  Content:");
            Console.WriteLineInterpolated($"{property}    ContentType: {value}{RequestDetails.Request.Content.GetContentType()}");
            Console.WriteLineInterpolated($"{property}    Body: {value}{RequestDetails.Request.Content.Body}");
        } else {
            Console.WriteLineInterpolated($"{property}  Content: {value}none");
        }

        // Proxy
        Console.WriteLineInterpolated($"{headerColor}Proxy:");
        Console.WriteLineInterpolated($"{property}  Bypass: {value}{RequestDetails.Proxy.Bypass}");
        Console.WriteLineInterpolated($"{property}  Host: {value}{RequestDetails.Proxy.Host}");
        Console.WriteLineInterpolated($"{property}  Username: {value}{RequestDetails.Proxy.Username}");
        Console.WriteLineInterpolated($"{property}  Password: {value}{RequestDetails.Proxy.Password}");
        Console.WriteLineInterpolated($"{property}  Ignore SSL: {value}{RequestDetails.Proxy.IgnoreSSL}");
    }
}