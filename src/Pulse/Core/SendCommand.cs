using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;
using PrettyConsole;
using static PrettyConsole.Console;
using System.Text.Json;

namespace Pulse.Core;

public sealed class SendCommand : Command {
	public static readonly SendCommand Singleton = new();

	private SendCommand() { }

	public override string Name => "";
	public override string Description => "";
	private const bool DefaultExportFullEquality = false;
	private const bool DefaultDisableExport = false;

	public override string Usage =>
	$"""
	Pulse [RequestFile] [Options]

	RequestFile:
	  path to .json request details file
	  - If you don't have one use the "generate-request" command
	Options:
	  -n, --number     : number of total requests
	  -m, --mode       : execution mode (sequential, parallel)
	                      * sequential = execute requests sequentially
	                      * parallel  = execute requests using maximum resources
	  -b, --batch      : batch size (only used in parallel mode)
	  --json           : try to format response content as JSON
	  -f               : use full equality (slower)
	  --no-export      : don't export results
	  -v, --verbose    : display verbose output
	Special:
	  generate-request : use as command - generated sample file
	  --noop           : print selected configuration but don't run
	  -u, --url        : override url of the request
	Defaults:
	  -n, --number     = {ParametersBase.DefaultNumberOfRequests}
	  -m, --mode       = {ParametersBase.DefaultExecutionMode}
	  -f               = {DefaultExportFullEquality}
	  --no-export      = {DefaultDisableExport}
	  -v, --verbose    = {false}
	""";

	internal static ParametersBase ParseParametersArgs(Arguments args) {
		args.TryGetValue(["n", "number"], ParametersBase.DefaultNumberOfRequests, out int requests);
		requests = Math.Max(requests, 1);
		args.TryGetEnum(["m", "mode"], ParametersBase.DefaultExecutionMode, true, out ExecutionMode mode);
		args.TryGetValue(["b", "batch"], ParametersBase.DefaultBatchSize, out int batchSize);
		batchSize = Math.Max(batchSize, 1);
		bool formatJson = args.HasFlag("json");
		bool exportFullEquality = args.HasFlag("f");
		bool disableExport = args.HasFlag("no-export");
		bool noop = args.HasFlag("noop");
		bool verbose = args.HasFlag("v") || args.HasFlag("verbose");
		return new() {
			Requests = requests,
			ExecutionMode = mode,
			BatchSize = batchSize,
			FormatJson = formatJson,
			UseFullEquality = exportFullEquality,
			Export = !disableExport,
			NoOp = noop,
			Verbose = verbose
		};
	}

	internal static Result<RequestDetails> GetRequestDetails(string requestSource, Arguments args) {
		var path = Path.GetFullPath(requestSource);
		var result = JsonContext.TryGetRequestDetailsFromFile(path);
		if (args.TryGetValue(["u", "url"], out string url)) {
			result.Value!.Request.Url = url;
		}
		return result;
	}

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		Services.Shared = new Services(new Parameters(new ParametersBase()));
		if (!args.TryGetValue(0, out string rf)) {
			WriteLineError("request file or command name must be provided!" * Color.Red);
			return 1;
		}

		if (string.Equals(rf, "generate-request", StringComparison.InvariantCultureIgnoreCase)) {
			try {
				var path = Path.Join(Directory.GetCurrentDirectory(), "request-sample.json");
				var json = JsonSerializer.Serialize(new RequestDetails(), JsonContext.Default.RequestDetails);
				await File.WriteAllTextAsync(path, json, Services.Shared.Parameters.CancellationTokenSource.Token);
				WriteLine(["Sample request generated at ", path * Color.Yellow]);
				return 0;
			} catch (Exception e) {
				WriteLineError(e.Message * Color.Red);
				return 1;
			}
		}

		var parametersBase = ParseParametersArgs(args);
		var requestDetailsResult = GetRequestDetails(rf, args);

		if (requestDetailsResult.IsFail) {
			WriteLineError(requestDetailsResult.Message * Color.Red);
			return 1;
		}

		var requestDetails = requestDetailsResult.Value!;
		Services.Shared.OverrideParameters(new Parameters(parametersBase));
		var @params = Services.Shared.Parameters;

		if (@params.NoOp) {
			PrintConfiguration(@params, requestDetails);
			return 0;
		}

		await Pulse.RunAsync(@params, requestDetails);

		return 0;
	}

	internal static void PrintConfiguration(Parameters parameters, RequestDetails requestDetails) {
		Color headerColor = Color.Cyan;
		Color property = Color.DarkGray;
		Color value = Color.White;

		// System
		if (parameters.Verbose) {
			WriteLine("System:" * headerColor);
			WriteLine(["  CPU Cores: " * property, $"{Environment.ProcessorCount}" * value]);
			WriteLine(["  OS: " * property, $"{Environment.OSVersion}" * value]);
		}

		// Options
		WriteLine("Options:" * headerColor);
		WriteLine(["  Request Count: " * property, $"{parameters.Requests}" * value]);
		WriteLine(["  Execution Mode: " * property, $"{parameters.ExecutionMode}" * value]);
#pragma warning disable IDE0002
		if (parameters.BatchSize is not Parameters.DefaultBatchSize) {
			WriteLine(["  Batch Size: " * property, $"{parameters.BatchSize}" * value]);
		}
#pragma warning restore IDE0002
		WriteLine(["  Format JSON: " * property, $"{parameters.FormatJson}" * value]);
		WriteLine(["  Export Full Equality: " * property, $"{parameters.UseFullEquality}" * value]);
		WriteLine(["  Export: " * property, $"{parameters.Export}" * value]);
		WriteLine(["  Verbose: " * property, $"{parameters.Verbose}" * value]);

		// Request
		WriteLine("Request:" * headerColor);
		WriteLine(["  URL: " * property, requestDetails.Request.Url * value]);
		WriteLine(["  Method: " * property, requestDetails.Request.Method.ToString() * value]);
		WriteLine("  Headers:" * Color.Yellow);
		if (requestDetails.Request.Headers.Count > 0) {
			foreach (var header in requestDetails.Request.Headers) {
				if (header.Value is null) {
					continue;
				}
				WriteLine(["    ", header.Key * property, ": ", header.Value.Value.ToString() * value]);
			}
		}
		if (requestDetails.Request.Content.Body.HasValue) {
			WriteLine("  Content:" * Color.Yellow);
			WriteLine(["    ContentType: " * property, requestDetails.Request.Content.GetContentType() * value]);
			WriteLine(["    Body: " * property, requestDetails.Request.Content.Body.ToString()! * value]);
		} else {
			WriteLine(["  Content: " * Color.Yellow, "none" * value]);
		}

		// Proxy
		WriteLine("Proxy:" * headerColor);
		WriteLine(["  Bypass: " * property, $"{requestDetails.Proxy.Bypass}" * value]);
		WriteLine(["  Host: " * property, requestDetails.Proxy.Host * value]);
		WriteLine(["  Username: " * property, requestDetails.Proxy.Username * value]);
		WriteLine(["  Password: " * property, requestDetails.Proxy.Password * value]);
		WriteLine(["  Ignore SSL: " * property, $"{requestDetails.Proxy.IgnoreSSL}" * value]);
	}
}