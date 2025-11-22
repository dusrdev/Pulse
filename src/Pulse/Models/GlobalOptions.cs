namespace Pulse.Models;

/// <summary>
/// Global options that can be used across commands.
/// </summary>
/// <param name="Format">The output format to use</param>
/// <param name="Quiet">Suppress progress output on stderr (only fatal errors will be shown)</param>
internal record GlobalOptions(OutputFormat Format, bool Quiet);