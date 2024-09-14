using Pulse.Configuration;
using static PrettyConsole.Console;
using PrettyConsole;
using Sharpify.CommandLineInterface;
using Sharpify;

namespace Pulse.Command;

public sealed class DefaultCommand : Sharpify.CommandLineInterface.Command {
	public override string Name => "Default";

	public override string Description => "Runs the test";

	public override string Usage =>
	"""
	Options:
		-d				: use config from file			(flag:default=off)
		-n				: number of total requests  	(int:default=100)
		-c				: concurrency mode 				(Maximum/Limited/Disabled:default=Maximum)
		-b				: amount of concurrent requests (int>1) applies to -c Limited
		-r				: use resilience				(flag:default=off)
		--no-export 	: don't export results			(flag)
	""";

	public static Config CreateFromArgs(Arguments args) {
		args.TryGetValue("n", 100, out int n);
		args.TryGetEnum("c", ConcurrencyMode.Maximum, true, out var concurrencyMode);
		args.TryGetValue("b", 1, out int concurrentRequests);
		bool useResilience = args.HasFlag("r");
		bool bypassExport = args.HasFlag("no-export");


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

		var pulseRunner = PulseRunner.Match(config);

		var pulseResult = await pulseRunner.RunAsync(requestDetails);

		return 0;
	}
}