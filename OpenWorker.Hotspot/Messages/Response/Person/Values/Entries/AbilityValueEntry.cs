using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Hotspot.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct AbilityValueEntry
{
    [field: FieldOffset(0)]
    public ulong Value { get; }

    [field: FieldOffset(0)]
    public int Current { get; }

    [field: FieldOffset(4)]
    public int Max { get; }

    public AbilityValueEntry(AbilityComponentEntry entry)
    {
        Current = entry.Current;
        Max = entry.Max;
    }

    public AbilityValueEntry(BinaryReader reader)
    {
        Value = reader.ReadUInt64();
    }
}