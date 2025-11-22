namespace Pulse.Models;

/// <summary>
/// Global options that can be used across commands.
/// </summary>
/// <param name="format">The output format to use</param>
internal record GlobalOptions(OutputFormat Format);