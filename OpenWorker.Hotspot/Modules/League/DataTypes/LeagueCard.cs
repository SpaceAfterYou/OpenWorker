using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly struct LeagueCard(BinaryReader reader)
{
    [field: FieldOffset(0)]
    public int Value { get; } = reader.ReadInt32();

    [field: FieldOffset(0)]
    public short Logo { get; init; }

    [field: FieldOffset(2)]
    public short Border { get; init; }
}