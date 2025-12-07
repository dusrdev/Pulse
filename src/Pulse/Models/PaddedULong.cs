using System.Runtime.InteropServices;

namespace Pulse.Models;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct PaddedULong {
    public ulong Value;
}