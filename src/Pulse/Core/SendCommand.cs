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
	  -n            : number of total requests	(int:default=100)
	  -c            : concurrency mode	(Maximum/Limited/Disabled:default=Maximum)
	  -b            : amount of concurrent requests (int>1) applies to -c Limited
	  -r            : use resilience	(flag:default=off)
	  -e            : export check full equality (slower)
	  --no-export   : don't export results	(flag)
	""";

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (args.HasFlag("d")) {
			string paramsPath = Utils.Env.PathInBaseDirectory("parameters.json");
			var parameters = Loader.Load(paramsPath, JsonContext.Default.ParametersBase, ParametersBase.Default);
			Services.Instance.Parameters.ModifyFromBase(parameters);
		} else {
			Services.Instance.Parameters.ModifyFromArgs(args);
		}

		string requestPath = Utils.Env.PathInBaseDirectory("request.json");
		var requestDetails = Loader.Load(requestPath, JsonContext.Default.RequestDetails, RequestDetails.Default);

		var @params = Services.Instance.Parameters;

		using var pulseRunner = AbstractPulse.Match(@params, requestDetails);

		await pulseRunner.RunAsync();

		return 0;
	}
}