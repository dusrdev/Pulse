using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Schema;

using ConsoleAppFramework;

using Pulse.Configuration;
using Pulse.Models;

namespace Pulse.Core;

/// <summary>
/// Commands
/// </summary>
internal static class Commands {
    public const string Version = "2.0.0.0";

    /// <summary>
    /// Pulse - A hyper fast general purpose HTTP request tester
    /// </summary>
    /// <param name="context"></param>
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
    public static async Task<int> Root(ConsoleAppContext context, [Argument] string requestFile,
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
        if (context.GlobalOptions is not GlobalOptions options) {
            throw new InvalidCastException();
        }
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
            OutputFormat = options.Format,
            Quiet = options.Quiet,
            OutputFolder = output
        };

        var requestFilePath = Path.GetFullPath(requestFile);

        if (!InputJsonContext.TryGetRequestDetailsFromFile(requestFilePath, out RequestDetails requestDetails)) {
            Console.WriteLineInterpolated(OutputPipe.Error, $"Failed to retrieve and parse request file from {Markup.Underline}{Yellow}{requestFilePath}{Markup.ResetUnderline}");
            return 1;
        }

        if (url is not null) {
            requestDetails.Request.Url = url;
        }

        var @params = new Parameters(parametersBase, ct);

        if (@params.NoOp) {
            PrintConfiguration(@params, requestDetails);
            return 0;
        }

        await Pulse.RunAsync(@params, requestDetails).ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// Checks whether there is a new version out on GitHub releases.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> CheckForUpdates(ConsoleAppContext context, CancellationToken ct = default) {
        if (context.GlobalOptions is not GlobalOptions options) {
            throw new InvalidCastException();
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/dusrdev/Pulse/releases/latest");
        using var response = await client.SendAsync(message, ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) {
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!DefaultJsonContext.TryDeserializeVersion(json, out var remoteVersion)) {
                Console.WriteLineInterpolated(OutputPipe.Error, $"Failed to retrieve version from remote.");
                return 1;
            }
            ArgumentNullException.ThrowIfNull(remoteVersion);
            var currentVersion = System.Version.Parse(Version);

            var outputModel = new CheckForUpdatesModel {
                CurrentVersion = currentVersion,
                RemoteVersion = remoteVersion,
                UpdateRequired = currentVersion < remoteVersion
            };

            outputModel.Output(options.Format);
            return 0;
        }

        Console.WriteLineInterpolated(OutputPipe.Error, $"Failed to check for updates - server response was not success");
        return 1;
    }

    /// <summary>
    /// Print the terms of use.
    /// </summary>
    /// <returns></returns>
    public static int TermsOfUse(ConsoleAppContext context) {
        if (context.GlobalOptions is not GlobalOptions options) {
            throw new InvalidCastException();
        }
        var model = new TermsOfServiceModel();
        model.Output(options.Format);
        return 0;
    }

    /// <summary>
    /// Generate a json schema for a request file.
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
        await File.WriteAllTextAsync(path, schema, ct).ConfigureAwait(false);
        Console.WriteLineInterpolated($"Schema generated at {Markup.Underline}{Yellow}{path}{Markup.ResetUnderline}");
        return 0;
    }

    /// <summary>
    /// Generate sample request file.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="directory">-d, Configures in which directory [will default to current]</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> GetSample(ConsoleAppContext context, string? directory = null, CancellationToken ct = default) {
        if (context.GlobalOptions is not GlobalOptions options) {
            throw new InvalidCastException();
        }
        directory ??= Directory.GetCurrentDirectory();
        var path = Path.Join(directory, "sample.json");
        var json = JsonSerializer.Serialize(new RequestDetails(), InputJsonContext.Default.RequestDetails);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
        var output = new GetSampleModel {
            Path = path
        };
        output.Output(options.Format);
        return 0;
    }

    /// <summary>
    /// Prints the configuration.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="requestDetails"></param>
    internal static void PrintConfiguration(Parameters parameters, RequestDetails requestDetails) {
        var configuration = new RunConfiguration {
            Parameters = parameters,
            RequestDetails = requestDetails
        };

        configuration.Output(parameters.OutputFormat);
    }
}
