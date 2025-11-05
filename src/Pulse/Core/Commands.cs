using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Schema;

using ConsoleAppFramework;

using Pulse.Configuration;

namespace Pulse.Core;

/// <summary>
/// Commands
/// </summary>
public static class Commands {
    public const string VERSION = "2.0.0.0";

    /// <summary>
    /// Pulse - A hyper fast general purpose HTTP request tester
    /// </summary>
    /// <param name="requestFile">Path to .json request details file [use "get-sample" if you don't have one]</param>
    /// <param name="json">Try to format response content as JSON</param>
    /// <param name="raw">Export raw results [without wrapping in custom HTML]</param>
    /// <param name="fullEquality">-f, Use full equality [slower]</param>
    /// <param name="noExport">Don't export results</param>
    /// <param name="verbose">-v, Display verbose output</param>
    /// <param name="noOp">Print selected configuration but don't run</param>
    /// <param name="output">-o, Output folder</param>
    /// <param name="delay">-d, Delay in milliseconds between requests</param>
    /// <param name="connections">-c, Maximum number of parallel requests</param>
    /// <param name="url">-u, Override the url of the request</param>
    /// <param name="number">-n, Number of total requests</param>
    /// <param name="timeout">-t, Timeout in milliseconds</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> Root([Argument] string requestFile,
                                        bool json,
                                        bool raw,
                                        bool fullEquality,
                                        bool noExport,
                                        bool verbose,
                                        bool noOp,
                                        string output = "results",
                                        int delay = -1,
                                        int? connections = null,
                                        string? url = null,
                                        [Range(1, int.MaxValue)] int number = 1,
                                        int timeout = -1,
                                        CancellationToken ct = default) {
        connections ??= number;

        var parametersBase = new ParametersBase {
            Requests = number,
            TimeoutInMs = timeout,
            DelayInMs = delay,
            Connections = connections.Value,
            FormatJson = json,
            ExportRaw = raw,
            UseFullEquality = fullEquality,
            Export = !noExport,
            NoOp = noOp,
            Verbose = verbose,
            OutputFolder = output
        };

        var requestFilePath = Path.GetFullPath(requestFile);

        if (!InputJsonContext.TryGetRequestDetailsFromFile(requestFilePath, out var requestDetails)) {
            WriteLine(OutputPipe.Error, $"Failed to retrieve and parse request file from {Yellow}{requestFilePath}");
            return 1;
        }
        ArgumentNullException.ThrowIfNull(requestDetails);
        if (url is not null) {
            requestDetails.Request.Url = url;
        }

        var @params = new Parameters(parametersBase, ct);

        if (@params.NoOp) {
            PrintConfiguration(@params, requestDetails);
            return 0;
        }

        WriteLine($"{Helper.GetMethodBasedColor(requestDetails.Request.Method.Method)}{requestDetails.Request.Method.Method}{Default} => {requestDetails.Request.Url}");
        await Pulse.RunAsync(@params, requestDetails);
        return 0;
    }

    /// <summary>
    /// Checks whether there is a new version out on GitHub releases.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> CheckForUpdates(CancellationToken ct = default) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/dusrdev/Pulse/releases/latest");
        using var response = await client.SendAsync(message, ct);
        if (response.IsSuccessStatusCode) {
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!DefaultJsonContext.TryDeserializeVersion(json, out var remoteVersion)) {
                WriteLine(OutputPipe.Error, $"Failed to retrieve version from remote.");
                return 1;
            }
            ArgumentNullException.ThrowIfNull(remoteVersion);
            var currentVersion = Version.Parse(VERSION);
            if (currentVersion < remoteVersion) {
                WriteLine($"{Yellow}A new version of Pulse is available!");
                WriteLine($"Your version: {Yellow}{VERSION}");
                WriteLine($"Latest version: {Green}{remoteVersion}");
                NewLine();
                WriteLine($"Download from https://github.com/dusrdev/Pulse/releases/latest");
            } else {
                WriteLine($"{Green}You are using the latest version of Pulse.");
            }
            return 0;
        } else {
            WriteLine(OutputPipe.Error, $"Failed to check for updates - server response was not success");
            return 1;
        }
    }

    /// <summary>
    /// Print the terms of use.
    /// </summary>
    /// <returns></returns>
    public static int TermsOfUse() {
        WriteLine(
            $"""
			By using this tool you agree to take full responsibility for the consequences of its use.

			Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's
			responsibility to obey all applicable local, state and federal laws.
			Developers assume no liability and are not responsible for any misuse or damage caused by this program.
			"""
        );
        return 0;
    }

    /// <summary>
    /// Generate a json schema file
    /// </summary>
    /// <param name="directory">-d, Configures in which directory [will default to current]</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> GetSchema(string? directory = null, CancellationToken ct = default) {
        directory ??= Directory.GetCurrentDirectory();
        var path = Path.Join(directory, "schema.json");
        var options = new JsonSchemaExporterOptions {
            TreatNullObliviousAsNonNullable = true,
        };
        var schema = InputJsonContext.Default.RequestDetails.GetJsonSchemaAsNode(options).ToString();
        await File.WriteAllTextAsync(path, schema, ct);
        WriteLine($"Schema generated at {Yellow}{path}");
        return 0;
    }

    /// <summary>
    /// Generate sample request file
    /// </summary>
    /// <param name="directory">-d, Configures in which directory [will default to current]</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> GetSample(string? directory = null, CancellationToken ct = default) {
        directory ??= Directory.GetCurrentDirectory();
        var path = Path.Join(directory, "sample.json");
        var json = JsonSerializer.Serialize(new RequestDetails(), InputJsonContext.Default.RequestDetails);
        await File.WriteAllTextAsync(path, json, ct);
        WriteLine($"Sample request generated at {Yellow}{path}");
        return 0;
    }

    /// <summary>
    /// Prints the configuration
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    internal static void PrintConfiguration(Parameters parameters, RequestDetails requestDetails) {
        Color headerColor = Cyan;
        Color property = DarkGray;
        Color value = White;

        // Options
        WriteLine($"{headerColor}Options:");
        WriteLine($"{property}  Request count: {value}{parameters.Requests}");
        WriteLine($"{property}  Concurrent connections: {value}{parameters.Connections}");
        WriteLine($"{property}  Delay: {value}{parameters.DelayInMs}ms");
        WriteLine($"{property}  Timeout: {value}{parameters.TimeoutInMs}");
        WriteLine($"{property}  Export Raw: {value}{parameters.ExportRaw}");
        WriteLine($"{property}  Format JSON: {value}{parameters.FormatJson}");
        WriteLine($"{property}  Export Full Equality: {value}{parameters.UseFullEquality}");
        WriteLine($"{property}  Export: {value}{parameters.Export}");
        WriteLine($"{property}  Verbose: {value}{parameters.Verbose}");
        WriteLine($"{property}  Output Folder: {value}{parameters.OutputFolder}");

        // Request
        WriteLine($"{headerColor}Request:");
        WriteLine($"{property}  URL: {value}{requestDetails.Request.Url}");
        WriteLine($"{property}  Method: {value}{requestDetails.Request.Method}");
        WriteLine($"{Yellow}  Headers:");
        if (requestDetails.Request.Headers.Count > 0) {
            foreach (var header in requestDetails.Request.Headers) {
                if (header.Value is null) {
                    continue;
                }
                WriteLine($"{property}    {header.Key}: {value}{header.Value.Value}");
            }
        }
        if (requestDetails.Request.Content.Body.HasValue) {
            WriteLine($"{Yellow}  Content:");
            WriteLine($"{property}    ContentType: {value}{requestDetails.Request.Content.GetContentType()}");
            WriteLine($"{property}    Body: {value}{requestDetails.Request.Content.Body}");
        } else {
            WriteLine($"{property}  Content: {value}none");
        }

        // Proxy
        WriteLine($"{headerColor}Proxy:");
        WriteLine($"{property}  Bypass: {value}{requestDetails.Proxy.Bypass}");
        WriteLine($"{property}  Host: {value}{requestDetails.Proxy.Host}");
        WriteLine($"{property}  Username: {value}{requestDetails.Proxy.Username}");
        WriteLine($"{property}  Password: {value}{requestDetails.Proxy.Password}");
        WriteLine($"{property}  Ignore SSL: {value}{requestDetails.Proxy.IgnoreSSL}");
    }
}
