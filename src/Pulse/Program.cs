using System.Text.Json;

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

app.Add("cli-schema", () => {
	CommandHelpDefinition[] schema = app.GetCliSchema();
	ReadOnlySpan<char> json = JsonSerializer.Serialize(schema, CliSchemaJsonSerializerContext.Default.CommandHelpDefinitionArray);
	Console.WriteLine(json);
	return 0;
});

await app.RunAsync(args).ConfigureAwait(false);