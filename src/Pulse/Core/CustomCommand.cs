using Pulse.Configuration;
using Sharpify.CommandLineInterface;
using Sharpify;

using static PrettyConsole.Console;
using PrettyConsole;

namespace Pulse.Core;

public sealed class CustomCommand : Command {
	public static readonly CustomCommand Singleton = new();

	private CustomCommand() { }

	public override string Name => "Custom";

	public override string Description => "Like \"Send\" but with url and proxy customization (Uses GET method)";

	public override string Usage =>
	"""
	[url] [Options]

	Options:
	  --proxy       : proxy host
	  --username    : proxy username
	  --password    : proxy password
	  -d            : use config from file	(flag:default=off)
	  -n            : number of total requests	(int:default=100)
	  -c            : concurrency mode	(Maximum/Limited/Disabled:default=Maximum)
	  -b            : amount of concurrent requests (int>1) applies to -c Limited
	  -r            : use resilience	(flag:default=off)
	  -e            : export check full equality (slower)
	  --no-export   : don't export results	(flag)
	""";

	public static Config CreateConfigFromArgs(Arguments args) {
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

	public static Result<RequestDetails> CreateRequestFromArgs(Arguments args) {
		if (!args.TryGetValue(0, out string url)) {
			WriteLineError("URL is a required parameter" * Color.Red);
			return Result.Fail();
		}

		if (!args.TryGetValue("proxy", out string proxy)) {
			WriteLine("Bypassing proxy");
			return Result.Ok(new RequestDetails() {
				BypassProxy = true,
				RequestMessage = new(HttpMethod.Get, url)
			});
		}

		if (!(args.TryGetValue("username", out string username) && args.TryGetValue("password", out string password))) {
			WriteLine("Using proxy without auth");
			return Result.Ok(new RequestDetails() {
				BypassProxy = false,
				ProxyHost = proxy,
				RequestMessage = new(HttpMethod.Get, url)
			});
		}

		return Result.Ok(new RequestDetails() {
			BypassProxy = false,
			ProxyHost = proxy,
			ProxyUsername = username,
			ProxyPassword = password,
			RequestMessage = new(HttpMethod.Get, url)
		});
	}

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		Config config;

		if (args.HasFlag("d")) {
			string configPath = Utils.Env.PathInBaseDirectory("config.json");
			config = Loader.Load(configPath, JsonContext.Default.Config, Config.Default);
		} else {
			config = CreateConfigFromArgs(args);
		}

		var requestResult = CreateRequestFromArgs(args);

		if (requestResult.IsFail) {
			return 1;
		}

		using var pulseRunner = AbstractPulse.Match(config, requestResult.Value!);

		await pulseRunner.RunAsync();

		return 0;
	}
}