using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// The logo and the border of a league, which go on the wire as one 32-bit value.
/// </summary>
[WireMessage]
[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly partial struct LeagueCard
{
    [field: FieldOffset(0)]
    public int Value { get; }

    [WireIgnore]
    [field: FieldOffset(0)]
    public short Logo { get; init; }

    [WireIgnore]
    [field: FieldOffset(2)]
    public short Border { get; init; }
}
