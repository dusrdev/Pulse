using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;
using PrettyConsole;
using static PrettyConsole.Console;
using System.Text.Json;
using System.Text.Json.Schema;

namespace Pulse.Core;

/// <summary>
/// The main command
/// </summary>
public sealed class SendCommand : Command {
	private readonly CancellationToken _cancellationToken;

	/// <summary>
	/// The constructor of the command
	/// </summary>
	/// <param name="cancellationToken">A global cancellation token source that will propagate to all tasks</param>
	public SendCommand(CancellationToken cancellationToken) {
		_cancellationToken = cancellationToken;
	}

	public override string Name => string.Empty;
	public override string Description => string.Empty;
	public override string Usage =>
	"""
	Pulse [RequestFile] [Options]

	RequestFile:
	  path to .json request details file
	  - If you don't have one use the "get-sample" command
	Options:
	  -n, --number     : number of total requests (default: 1)
	  -t, --timeout    : timeout in milliseconds (default: -1 - infinity)
	  -m, --mode       : execution mode (default: parallel)
	      * sequential = execute requests sequentially
		    --delay    : delay between requests in milliseconds (default: 0)
	      * parallel  = execute requests using maximum resources
		    -c         : max concurrent connections (default: infinity)
	  --json           : try to format response content as JSON
	  -f               : use full equality (slower - default: false)
	  --no-export      : don't export results (default: false)
	  -v, --verbose    : display verbose output (default: false)
	  -o, --output     : output folder (default: results)
	Special:
	  get-sample       : command - generates sample file
	  get-schema       : command - generates a json schema file
	  check-for-updates: command - checks for updates
	  terms-of-use     : print the terms of use
	  --noop           : print selected configuration but don't run
	  -u, --url        : override the url of the request
	  -h, --help       : print this help text
	""";

	internal static ParametersBase ParseParametersArgs(Arguments args) {
		args.TryGetValue(["n", "number"], ParametersBase.DefaultNumberOfRequests, out int requests);
		requests = Math.Max(requests, 1);
		args.TryGetValue(["t", "timeout"], ParametersBase.DefaultTimeoutInMs, out int timeoutInMs);
		bool batchSizeModified = false;
		int maxConnections = 0;
		int delayInMs = 0;
		args.TryGetEnum(["m", "mode"], ParametersBase.DefaultExecutionMode, true, out ExecutionMode mode);
		if (mode is ExecutionMode.Parallel) {
			if (args.TryGetValue("c", ParametersBase.DefaultMaxConnections, out maxConnections)) {
				batchSizeModified = true;
			}
		} else if (mode is ExecutionMode.Sequential) {
			args.TryGetValue("delay", 0, out delayInMs);
			delayInMs = Math.Max(0, delayInMs);
		}
		args.TryGetValue(["o", "output"], "results", out string outputFolder);
		maxConnections = Math.Max(maxConnections, ParametersBase.DefaultMaxConnections);
		bool formatJson = args.HasFlag("json");
		bool exportFullEquality = args.HasFlag("f");
		bool disableExport = args.HasFlag("no-export");
		bool noop = args.HasFlag("noop");
		bool verbose = args.HasFlag("v") || args.HasFlag("verbose");
		return new ParametersBase {
			Requests = requests,
			TimeoutInMs = timeoutInMs,
			DelayInMs = delayInMs,
			ExecutionMode = mode,
			MaxConnections = maxConnections,
			MaxConnectionsModified = batchSizeModified,
			FormatJson = formatJson,
			UseFullEquality = exportFullEquality,
			Export = !disableExport,
			NoOp = noop,
			Verbose = verbose,
			OutputFolder = outputFolder
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
		var result = InputJsonContext.TryGetRequestDetailsFromFile(path);
		if (args.TryGetValue(["u", "url"], out string url)) {
			result.Value!.Request.Url = url;
		}
		return result;
	}

	/// <summary>
	/// Executes the command
	/// </summary>
	/// <param name="args"></param>
	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (!args.TryGetValue(0, out string rf)) {
			WriteLine("request file or command name must be provided!" * Color.Red, OutputPipe.Error);
			return 1;
		}

		if (SubCommands.TryGetValue(rf, out var subCommand)) {
			try {
				await subCommand(_cancellationToken);
				return 0;
			} catch (Exception e) {
				WriteLine(e.Message * Color.Red, OutputPipe.Error);
				return 1;
			}
		}

		var parametersBase = ParseParametersArgs(args);
		var requestDetailsResult = GetRequestDetails(rf, args);

		if (requestDetailsResult.IsFail) {
			WriteLine(requestDetailsResult.Message * Color.Red, OutputPipe.Error);
			return 1;
		}

		var requestDetails = requestDetailsResult.Value!;
		var @params = new Parameters(parametersBase, _cancellationToken);

		if (@params.NoOp) {
			PrintConfiguration(@params, requestDetails);
			return 0;
		}

		WriteLine(Helper.CreateHeader(requestDetails.Request));
		await Pulse.RunAsync(@params, requestDetails);
		return 0;
	}

	internal static readonly Dictionary<string, Func<CancellationToken, ValueTask>> SubCommands = new(2, StringComparer.OrdinalIgnoreCase) {
		["get-sample"] = async token => {
			var path = Path.Join(Directory.GetCurrentDirectory(), "sample.json");
			var json = JsonSerializer.Serialize(new RequestDetails(), InputJsonContext.Default.RequestDetails);
			await File.WriteAllTextAsync(path, json, token);
			WriteLine(["Sample request generated at ", path * Color.Yellow]);
		},
		["get-schema"] = async token => {
			var path = Path.Join(Directory.GetCurrentDirectory(), "schema.json");
			var options = new JsonSchemaExporterOptions {
				TreatNullObliviousAsNonNullable = true,
			};
			var schema = InputJsonContext.Default.RequestDetails.GetJsonSchemaAsNode(options).ToString();
			await File.WriteAllTextAsync(path, schema, token);
			WriteLine(["Schema generated at ", path * Color.Yellow]);
		},
		["check-for-updates"] = async token => {
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "C# App");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/dusrdev/Pulse/releases/latest");
			using var response = await client.SendAsync(message, token);
			if (response.IsSuccessStatusCode) {
				var json = await response.Content.ReadAsStringAsync(token);
				var result = DefaultJsonContext.DeserializeVersion(json);
				if (result.IsFail) {
					return;
				}
				if (!Version.TryParse(result.Message, out Version? remoteVersion)) {
					WriteLine("Failed to parse remote version.", OutputPipe.Error);
					return;
				}
				var currentVersion = Version.Parse(Program.VERSION);
				if (currentVersion < remoteVersion) {
					WriteLine("A new version of Pulse is available!" * Color.Yellow);
					WriteLine(["Your version: ", Program.VERSION * Color.Yellow]);
					WriteLine(["Latest version: ", remoteVersion.ToString() * Color.Green]);
					NewLine();
					WriteLine("Download from https://github.com/dusrdev/Pulse/releases/latest");
				} else {
					WriteLine("You are using the latest version of Pulse." * Color.Green);
				}
			} else {
				WriteLine("Failed to check for updates - server response was not success", OutputPipe.Error);
			}
		},
		["terms-of-use"] = _ => {
			Out.WriteLine(
				"""
				By using this tool you agree to take full responsibility for the consequences of its use.

				Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's
				responsibility to obey all applicable local, state and federal laws.
				Developers assume no liability and are not responsible for any misuse or damage caused by this program.
				"""
			);
			return ValueTask.CompletedTask;
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

		// Options
		WriteLine("Options:" * headerColor);
		WriteLine(["  Request count: " * property, $"{parameters.Requests}" * value]);
		WriteLine(["  Timeout: " * property, $"{parameters.TimeoutInMs}" * value]);
		WriteLine(["  Execution mode: " * property, $"{parameters.ExecutionMode}" * value]);
		if (parameters.ExecutionMode is ExecutionMode.Parallel && parameters.MaxConnectionsModified) {
			WriteLine(["  Maximum concurrent connections: " * property, $"{parameters.MaxConnections}" * value]);
		}
		WriteLine(["  Format JSON: " * property, $"{parameters.FormatJson}" * value]);
		WriteLine(["  Export Full Equality: " * property, $"{parameters.UseFullEquality}" * value]);
		WriteLine(["  Export: " * property, $"{parameters.Export}" * value]);
		WriteLine(["  Verbose: " * property, $"{parameters.Verbose}" * value]);
		WriteLine(["  Output Folder: " * property, parameters.OutputFolder * value]);

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