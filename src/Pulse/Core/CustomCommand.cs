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

	Proxy Parameters:
	  --ignore-ssl  : ignore ssl verification (flag:default=off)
	  --proxy       : proxy host
	  --username    : proxy username
	  --password    : proxy password

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

	public static Result<RequestDetails> CreateRequestFromArgs(Arguments args) {
		if (!args.TryGetValue(0, out string url)) {
			WriteLineError("URL is a required parameter" * Color.Red);
			return Result.Fail();
		}

		bool ignoreSSL = args.HasFlag("ignore-ssl");

		if (!args.TryGetValue("proxy", out string proxy)) {
			WriteLine("Bypassing proxy");
			return Result.Ok(new RequestDetails {
				Proxy = new Proxy {
					Bypass = true,
					IgnoreSSL = ignoreSSL
				},
				Request = new Request {
					Url = url,
					Method = HttpMethod.Get
				}
			});
		}

		if (!(args.TryGetValue("username", out string username) && args.TryGetValue("password", out string password))) {
			WriteLine("Using proxy without auth");
			return Result.Ok(new RequestDetails {
				Proxy = new Proxy {
					Bypass = false,
					IgnoreSSL = ignoreSSL,
					Host = proxy,
				},
				Request = new Request {
					Url = url,
					Method = HttpMethod.Get
				}
			});
		}

		return Result.Ok(new RequestDetails {
			Proxy = new Proxy {
				Bypass = false,
				IgnoreSSL = ignoreSSL,
				Host = proxy,
				Username = username,
				Password = password
			},
			Request = new Request {
				Url = url,
				Method = HttpMethod.Get
			}
		});
	}

	public override async ValueTask<int> ExecuteAsync(Arguments args) {
		if (args.HasFlag("d")) {
			string paramsPath = Utils.Env.PathInBaseDirectory("parameters.json");
			using var fromFile = new SerializableObject<ParametersBase>(paramsPath, ParametersBase.Default, JsonContext.Default.ParametersBase);
			var parameters = fromFile.Value;
			Services.Instance.Parameters.ModifyFromBase(parameters);
		} else {
			Services.Instance.Parameters.ModifyFromArgs(args);
		}

		var requestResult = CreateRequestFromArgs(args);

		if (requestResult.IsFail) {
			return 1;
		}

		var @params = Services.Instance.Parameters;

		using var pulseRunner = AbstractPulse.Match(@params, requestResult.Value!);

		await pulseRunner.RunAsync();

		return 0;
	}
}