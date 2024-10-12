using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;
using PrettyConsole;
using static PrettyConsole.Console;

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
	  -m, --mode       : execution mode (sequential, bounded, unbounded)
	                      * sequential = execute requests sequentially
	                      * bounded    = execute requests such that only 1 requests per core is allowed
	                      * unbounded  = execute requests using maximum resources
	  -f               : use full equality (slower)
	  --no-export      : don't export results

	Special:
	  generate-request : use as command - generated sample file
	  --noop           : print selected configuration but don't run

	Defaults:
	  -n, --number     = {ParametersBase.DefaultNumberOfRequests}
	  -m, --mode       = {ParametersBase.DefaultExecutionMode}
	  -f               = {DefaultExportFullEquality}
	  --no-export      = {DefaultDisableExport}
	""";

	internal static ParametersBase ParseParametersArgs(Arguments args) {
		args.TryGetValue(["n", "number"], ParametersBase.DefaultNumberOfRequests, out int requests);
		requests = Math.Max(requests, 1);
		args.TryGetEnum(["m", "mode"], ParametersBase.DefaultExecutionMode, true, out ExecutionMode mode);
		bool exportFullEquality = args.HasFlag("f");
		bool disableExport = args.HasFlag("no-export");
		bool noop = args.HasFlag("noop");
		return new() {
			Requests = requests,
			ExecutionMode = mode,
			UseFullEquality = exportFullEquality,
			NoExport = disableExport,
			NoOp = noop
		};
	}

	internal static Result<RequestDetails> GetRequestDetails(string requestSource) {
		var path = Path.GetFullPath(requestSource);
		if (!File.Exists(path)) {
			return Result.Fail("Request file count not be found.");
		}
		try {
			using var detailsFromFile = new SerializableObject<RequestDetails>(path, new(), JsonContext.Default.RequestDetails);
			return Result.Ok(detailsFromFile.Value);
		} catch (Exception e) {
			return Result.Fail(e.Message);
		}
	}

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (!args.TryGetValue(0, out string rf)) {
			WriteLineError("request file or command name must be provided!" * Color.Red);
			return 1;
		}

		if (string.Equals(rf, "generate-request", StringComparison.InvariantCultureIgnoreCase)) {
			var path = Utils.Env.PathInBaseDirectory("request-sample.json");
			using var detailsFromFile = new SerializableObject<RequestDetails>(path, new(), JsonContext.Default.RequestDetails);
			WriteLine(["Sample request generated at ", path * Color.Yellow]);
			return 0;
		}

		var parametersBase = ParseParametersArgs(args);
		var requestDetailsResult = GetRequestDetails(rf);

		if (requestDetailsResult.IsFail) {
			WriteLineError(requestDetailsResult.Message * Color.Red);
			return 1;
		}

		var requestDetails = requestDetailsResult.Value!;

		Services.Instance.Parameters.ModifyFromBase(parametersBase);
		var @params = Services.Instance.Parameters;

		if (@params.NoOp) {
			PrintConfiguration(@params, requestDetails);
			return 0;
		}

		using var pulseRunner = AbstractPulse.Match(@params, requestDetails);

		await pulseRunner.RunAsync();

		return 0;
	}

	internal static void PrintConfiguration(Parameters parameters, RequestDetails requestDetails) {
		Color headerColor = Color.Cyan;
		Color property = Color.DarkGray;
		Color value = Color.White;

		// System
		WriteLine("System:" * headerColor);
		WriteLine(["  CPU Cores: " * property, $"{Environment.ProcessorCount}" * value]);
		WriteLine(["  OS: " * property, $"{Environment.OSVersion}" * value]);

		// Options
		WriteLine("Options:" * headerColor);
		WriteLine(["  Request Count: " * property, $"{parameters.Requests}" * value]);
		WriteLine(["  Execution Mode: " * property, $"{parameters.ExecutionMode}" * value]);
		WriteLine(["  Export Full Equality: " * property, $"{parameters.UseFullEquality}" * value]);
		WriteLine(["  No Export: " * property, $"{parameters.NoExport}" * value]);

		// Request
		WriteLine("Request:" * headerColor);
		WriteLine(["  URL: " * property, requestDetails.Request.Url.ToStringOrDefault() * value]);
		WriteLine(["  Method: " * property, requestDetails.Request.Method.ToStringOrDefault() * value]);
		if (requestDetails.Request.Headers.Count > 0) {
			WriteLine("  Headers:" * Color.Yellow);
			foreach (var header in requestDetails.Request.Headers) {
				WriteLine(["    ", header.Key.ToStringOrDefault(), ": ", header.Value.ToStringOrDefault() * value]);
			}
		} else {
			WriteLine(["  Headers: " * property, Constants.EmptyValue * value]);
		}
		WriteLine(["  Body: " * property, requestDetails.Request.Body.ToStringOrDefault() * value]);

		// Proxy
		WriteLine("Proxy:" * headerColor);
		WriteLine(["  Bypass: " * property, $"{requestDetails.Proxy.Bypass}" * value]);
		WriteLine(["  Host: " * property, requestDetails.Proxy.Host.ToStringOrDefault() * value]);
		WriteLine(["  Username: " * property, requestDetails.Proxy.Username.ToStringOrDefault() * value]);
		WriteLine(["  Password: " * property, requestDetails.Proxy.Password.ToStringOrDefault() * value]);
		WriteLine(["  Ignore SSL: " * property, $"{requestDetails.Proxy.IgnoreSSL}" * value]);
	}
}