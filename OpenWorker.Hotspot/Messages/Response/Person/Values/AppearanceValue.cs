using System.Runtime.InteropServices;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
public readonly struct AppearanceValue
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

    public AppearanceValue(BinaryReader reader)
    {
        Value = reader.ReadUInt64();
    }
}
