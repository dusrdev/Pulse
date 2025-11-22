using ConsoleAppFramework;

using Pulse.Core;
using Pulse.Models;

ConsoleApp.Version = Commands.Version;

var app = ConsoleApp.Create();

app.UseFilter<GlobalExceptionHandler>();

app.ConfigureGlobalOptions((ref builder) => {
	var format = builder.AddGlobalOption("--output-format", description: "Output as PlainText|JSON", defaultValue: OutputFormat.PlainText);
	return new GlobalOptions(format);
});

app.Add("", Commands.Root);
app.Add("get-sample", Commands.GetSample);
app.Add("get-schema", Commands.GetSchema);
app.Add("check-for-updates", Commands.CheckForUpdates);
app.Add("terms-of-use", Commands.TermsOfUse);

var schemaCommand = new CliSchemaCommand(app);

app.Add("cli-schema", schemaCommand.Command);

await app.RunAsync(args).ConfigureAwait(false);