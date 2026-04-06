using System.Runtime.InteropServices;

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

    public PersonLeagueCardValueEntry(short logo, short border)
    {
        Logo = logo;
        Border = border;
    }

    public PersonLeagueCardValueEntry(BinaryReader reader)
    {
        Value = reader.ReadInt32();
    }
}
