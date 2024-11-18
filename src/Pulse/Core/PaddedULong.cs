using System.Runtime.InteropServices;

namespace Pulse.Core;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct PaddedULong {
	public ulong Value;
}