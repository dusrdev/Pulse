namespace Pulse.Models;

/// <summary>
/// Global options that can be used across commands.
/// </summary>
/// <param name="Format">The output format to use</param>
internal record GlobalOptions(OutputFormat Format);