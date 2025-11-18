using ConsoleAppFramework;

using Pulse.Core;

ConsoleApp.Version = Commands.VERSION;

var app = ConsoleApp.Create();

app.UseFilter<GlobalExceptionHandler>();

app.Add("", Commands.Root);
app.Add("get-sample", Commands.GetSample);
app.Add("get-schema", Commands.GetSchema);
app.Add("check-for-updates", Commands.CheckForUpdates);
app.Add("terms-of-use", Commands.TermsOfUse);

var schemaCommand = new CliSchemaCommand(app);

app.Add("cli-schema", schemaCommand.Command);

await app.RunAsync(args).ConfigureAwait(false);