using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;
using PrettyConsole;
using static PrettyConsole.Console;
using System.Text.Json;

namespace Pulse.Core;

/// <summary>
/// The main command
/// </summary>
public sealed class SendCommand : Command {
	private readonly CancellationToken _cancellationToken;

	/// <summary>
	/// The constructor of the command
	/// </summary>
	/// <param name="globalCTS">A global cancellation token source that will propagate to all tasks</param>
	public SendCommand(CancellationToken cancellationToken) {
		_cancellationToken = cancellationToken;
	}

	public override string Name => "";
	public override string Description => "";
	public override string Usage =>
	$"""
	Pulse [RequestFile] [Options]

	RequestFile:
	  path to .json request details file
	  - If you don't have one use the "get-sample" command
	Options:
	  -n, --number     : number of total requests (default: 1)
	  -m, --mode       : execution mode (default: parallel)
	                      * sequential = execute requests sequentially
	                      * parallel  = execute requests using maximum resources
	  -b, --batch      : batch size (only used in parallel mode)
	  --json           : try to format response content as JSON
	  -f               : use full equality (slower - default: false)
	  --no-export      : don't export results (default: false)
	  -v, --verbose    : display verbose output	(default: false)
	Special:
	  get-sample       : use as command - generated sample file
	  check-for-updates: use as command - checks for updates
	  --noop           : print selected configuration but don't run
	  -u, --url        : override url of the request
	  -h, --help       : print this help
	  --terms-of-use   : print terms of use
	""";

	internal static ParametersBase ParseParametersArgs(Arguments args) {
		args.TryGetValue(["n", "number"], ParametersBase.DefaultNumberOfRequests, out int requests);
		requests = Math.Max(requests, 1);
		bool batchSizeModified = false;
		args.TryGetEnum(["m", "mode"], ParametersBase.DefaultExecutionMode, true, out ExecutionMode mode);
		if (args.TryGetValue(["b", "batch"], ParametersBase.DefaultBatchSize, out int batchSize)) {
			batchSizeModified = true;
		}
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
			BatchSizeModified = batchSizeModified,
			FormatJson = formatJson,
			UseFullEquality = exportFullEquality,
			Export = !disableExport,
			NoOp = noop,
			Verbose = verbose
		};
	}

	/// <summary>
	/// Gets the request details from the specified file
	/// </summary>
	/// <param name="requestSource"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	internal static Result<RequestDetails> GetRequestDetails(string requestSource, Arguments args) {
		var path = Path.GetFullPath(requestSource);
		var result = JsonContext.TryGetRequestDetailsFromFile(path);
		if (args.TryGetValue(["u", "url"], out string url)) {
			result.Value!.Request.Url = url;
		}
		return result;
	}

	/// <summary>
	/// Executes the command
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (args.HasFlag("terms-of-use")) {
			Out.WriteLine(
				"""
				By using this tool you agree to take full responsibility for the consequences of its use.

				Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's
				responsibility to obey all applicable local, state and federal laws.
				Developers assume no liability and are not responsible for any misuse or damage caused by this program.
				"""
			);
			return 0;
		}

		if (!args.TryGetValue(0, out string rf)) {
			WriteLineError("request file or command name must be provided!" * Color.Red);
			return 1;
		}

		if (SubCommands.TryGetValue(rf, out var subCommand)) {
			try {
				await subCommand(_cancellationToken);
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
		var @params = new Parameters(parametersBase, _cancellationToken);

		if (@params.NoOp) {
			PrintConfiguration(@params, requestDetails);
			return 0;
		}


		await Pulse.RunAsync(@params, requestDetails);

		return 0;
	}

	internal static readonly Dictionary<string, Func<CancellationToken, ValueTask>> SubCommands = new(2, StringComparer.OrdinalIgnoreCase) {
		["get-sample"] = async (token) => {
			var path = Path.Join(Directory.GetCurrentDirectory(), "sample.json");
			var json = JsonSerializer.Serialize(new RequestDetails(), JsonContext.Default.RequestDetails);
			await File.WriteAllTextAsync(path, json, token);
			WriteLine(["Sample request generated at ", path * Color.Yellow]);
		},
		["check-for-updates"] = async (token) => {
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "C# App");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/dusrdev/Pulse/releases/latest");
			using var response = await client.SendAsync(message, token);
			if (response.IsSuccessStatusCode) {
				var json = await response.Content.ReadAsStringAsync(token);
				var result = JsonContext.DeserializeVersion(json);
				if (result.IsFail) {
					return;
				}
				var version = result.Message;
				if (string.Compare(Program.VERSION, version) < 0) {
					WriteLine("A new version of Pulse is available!" * Color.Yellow);
					WriteLine(["Your version: ", Program.VERSION * Color.Yellow]);
					WriteLine(["Latest version: ", version * Color.Green]);
					NewLine();
					WriteLine("Download from https://github.com/dusrdev/Pulse/releases/latest");
				} else {
					WriteLine("You are using the latest version of Pulse." * Color.Green);
				}
			} else {
				WriteLineError("Failed to check for updates - server response was not success");
			}
		}
	};

	/// <summary>
	/// Prints the configuration
	/// </summary>
	/// <param name="parameters"></param>
	/// <param name="requestDetails"></param>
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