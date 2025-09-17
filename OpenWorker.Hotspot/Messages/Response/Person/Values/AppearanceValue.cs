using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Components;

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

    public static AppearanceValue CreateLook(AppearanceComponent component)
    {
        return new AppearanceValue(
            component.HairStyle.Look,
            component.HairColor.Look,
            component.EyeColor.Look,
            component.SkinColor.Look
        );
    }

    public static AppearanceValue CreateShape(AppearanceComponent component)
    {
        return new AppearanceValue(
            component.HairStyle.Shape,
            component.HairColor.Shape,
            component.EyeColor.Shape,
            component.SkinColor.Shape
        );
    }

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