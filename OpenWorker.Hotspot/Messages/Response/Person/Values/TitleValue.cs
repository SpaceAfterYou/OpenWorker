using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct TitleValue
{
    [field: FieldOffset(0)]
    public long Value { get; }

    [field: FieldOffset(0)]
    public int Primary { get; }

    [field: FieldOffset(4)]
    public int Secondary { get; }

    public TitleValue(TitleComponent component)
    {
        Primary = component.Primary;
        Secondary = component.Secondary;
    }

    public TitleValue(BinaryReader reader)
    {
        Value = reader.ReadInt64();
    }
}