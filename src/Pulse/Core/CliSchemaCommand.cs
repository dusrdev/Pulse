using System.Text.Json;

using ConsoleAppFramework;

namespace Pulse.Core;

internal sealed class CliSchemaCommand {
    private readonly ConsoleApp.ConsoleAppBuilder _app;

    public CliSchemaCommand(ConsoleApp.ConsoleAppBuilder app) {
        _app = app;
    }

    /// <summary>
    /// Returns the usage schema for the app in JSON format.
    /// </summary>
    /// <returns></returns>
    public int Command() {
        CommandHelpDefinition[] schema = _app.GetCliSchema();
        ReadOnlySpan<char> json = JsonSerializer.Serialize(schema, CliSchemaJsonSerializerContext.Default.CommandHelpDefinitionArray);
        Console.WriteLine(json);
        return 0;
    }
}