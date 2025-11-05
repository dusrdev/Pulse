using System.Diagnostics.CodeAnalysis;

using ConsoleAppFramework;

using Pulse.Core;

internal partial class Program {
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Commands))]
    private static async Task Main(string[] args) {
        ConsoleApp.Version = Commands.VERSION;

        var app = ConsoleApp.Create();

        app.UseFilter<GlobalExceptionHandler>();

        app.Add("", Commands.Root);
        app.Add("get-sample", Commands.GetSample);
        app.Add("get-schema", Commands.GetSchema);
        app.Add("check-for-updates", Commands.CheckForUpdates);
        app.Add("terms-of-use", Commands.TermsOfUse);

        await app.RunAsync(args);
    }
}