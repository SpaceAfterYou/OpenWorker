using System.Runtime.InteropServices;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
public readonly struct AppearanceValue : IWireReadable<AppearanceValue>, IWireWritable
{
    [field: FieldOffset(0)]
    public ulong Value { get; init; }

    [field: FieldOffset(0)]
    public short HairStyle { get; }

    [field: FieldOffset(2)]
    public short HairColor { get; }

    [field: FieldOffset(4)]
    public short EyeColor { get; }

    [field: FieldOffset(6)]
    public short SkinColor { get; }

    public AppearanceValue(short hairStyle, short hairColor, short eyeColor, short skinColor)
    {
        HairStyle = hairStyle;
        HairColor = hairColor;
        EyeColor = eyeColor;
        SkinColor = skinColor;
    }

    private AppearanceValue(ulong value)
    {
        Value = value;
    }

    public static AppearanceValue Read(ref PacketReader reader) => new(reader.ReadUInt64());

    /// <summary>
    /// The four shorts as one 64-bit value.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteUInt64(Value);
    }
}
