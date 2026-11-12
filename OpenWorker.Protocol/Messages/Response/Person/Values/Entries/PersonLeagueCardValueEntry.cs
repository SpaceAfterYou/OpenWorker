using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
public readonly struct PersonLeagueCardValueEntry : IWireReadable<PersonLeagueCardValueEntry>, IWireWritable
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

    private PersonLeagueCardValueEntry(int value)
    {
        Value = value;
    }

    public static PersonLeagueCardValueEntry Read(ref PacketReader reader) => new(reader.ReadInt32());

    /// <summary>
    /// The logo and the border as one 32-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Value);
    }
}
