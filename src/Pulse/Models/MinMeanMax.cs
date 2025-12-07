namespace Pulse.Models;

internal readonly struct MinMeanMax {
    public required double Min { get; init; }
    public required double Mean { get; init; }
    public required double Max { get; init; }
}