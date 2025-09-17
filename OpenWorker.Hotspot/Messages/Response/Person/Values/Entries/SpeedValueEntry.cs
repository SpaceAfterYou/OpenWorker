using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Messages.Response.Person.Components.Entries;
using OpenWorker.Hotspot.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct SpeedValueEntry
{
    [field: FieldOffset(0)]
    public ulong Value { get; }

    [field: FieldOffset(0)]
    public float Move { get; }

    [field: FieldOffset(4)]
    public float Attack { get; }

    public SpeedValueEntry(SpeedComponentEntry entry)
    {
        Move = entry.Move;
        Attack = entry.Attack;
    }

    public SpeedValueEntry(BinaryReader reader)
    {
        Value = reader.ReadUInt64();
    }
}