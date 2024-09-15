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
	  -d            : use config from file	(flag:default=off)
	  -n            : number of total requests	(int:default=100)
	  -c            : concurrency mode	(Maximum/Limited/Disabled:default=Maximum)
	  -b            : amount of concurrent requests (int>1) applies to -c Limited
	  -r            : use resilience	(flag:default=off)
	  -e            : export check full equality (slower)
	  --no-export   : don't export results	(flag)
	""";

	public static Config CreateFromArgs(Arguments args) {
		args.TryGetValue("n", 100, out int n);
		args.TryGetEnum("c", ConcurrencyMode.Maximum, true, out var concurrencyMode);
		args.TryGetValue("b", 1, out int concurrentRequests);
		bool useResilience = args.HasFlag("r");
		bool bypassExport = args.HasFlag("no-export");

		if (args.HasFlag("e")) {
			Services.Instance.Parameters.UseFullEquality = true;
		}

		if (concurrencyMode is not ConcurrencyMode.Limited) {
			concurrentRequests = 1;
		}

		return new Config {
			Requests = n,
			ConcurrencyMode = concurrencyMode,
			ConcurrentRequests = concurrentRequests,
			UseResilience = useResilience,
			BypassExport = bypassExport
		};
	}

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		Config config;

		if (args.HasFlag("d")) {
			string configPath = Utils.Env.PathInBaseDirectory("config.json");
			config = Loader.Load(configPath, JsonContext.Default.Config, Config.Default);
		} else {
			config = CreateFromArgs(args);
		}

		string requestPath = Utils.Env.PathInBaseDirectory("request.json");
		var requestDetails = Loader.Load(requestPath, JsonContext.Default.RequestDetails, RequestDetails.Default);

		using var pulseRunner = AbstractPulse.Match(config, requestDetails);

		await pulseRunner.RunAsync();

		return 0;
	}
}