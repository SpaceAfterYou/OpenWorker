using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Hotspot.Modules.League.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly struct PersonLeagueCardValueEntry
{
    [field: FieldOffset(0)]
    public int Value { get; }

    [field: FieldOffset(0)]
    public short Logo { get; }

    [field: FieldOffset(2)]
    public short Border { get; }

    public PersonLeagueCardValueEntry(LeagueComponentCard value)
    {
        Logo = value.Emblem;
        Border = value.Border;
    }

    public PersonLeagueCardValueEntry(BinaryReader reader)
    {
        Value = reader.ReadInt32();
    }
}