using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;

namespace Pulse.Core;

public sealed class SendCommand : Command {
	public static readonly SendCommand Singleton = new();

	private SendCommand() {}

	public override string Name => "Send";

	public override string Description => "Main and default command";

	public override string Usage =>
	"""
	[Options]

	Options:

	Default Parameters:
	  -d            : use config from file	(flag:default=off)

	Custom Parameters:
	  -n, --number      : number of total requests	(int:default=100)
	  -c, --concurrency : concurrency mode	(Maximum/Limited/Disabled:default=Maximum)
	  -b                : amount of concurrent requests (int>1) applies to -c Limited
	  -r, --resilient   : use resilience	(flag:default=off)
	  -e                : export check full equality (slower)
	  --no-export       : don't export results	(flag)
	""";

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (args.HasFlag("d")) {
			string paramsPath = Utils.Env.PathInBaseDirectory("parameters.json");
			using var fromFile = new SerializableObject<ParametersBase>(paramsPath, ParametersBase.Default, JsonContext.Default.ParametersBase);
			var parameters = fromFile.Value;
			Services.Instance.Parameters.ModifyFromBase(parameters);
		} else {
			Services.Instance.Parameters.ModifyFromArgs(args);
		}

		string requestPath = Utils.Env.PathInBaseDirectory("request.json");
		using var detailsFromFile = new SerializableObject<RequestDetails>(requestPath, RequestDetails.Default, JsonContext.Default.RequestDetails);
		var requestDetails = detailsFromFile.Value;

		var @params = Services.Instance.Parameters;

		using var pulseRunner = AbstractPulse.Match(@params, requestDetails);

		await pulseRunner.RunAsync();

		return 0;
	}
}