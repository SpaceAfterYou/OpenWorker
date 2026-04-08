using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

/// <summary>
/// Three dwords passed by reference into mover vftable slot (see client <c>sub_95DAF0</c>, arg <c>&amp;v10</c>).
/// </summary>
public readonly struct AkashicSummonVirtualArgs(BinaryReader reader) : IWritableData
{
    public int Arg0 { get; init; } = reader.ReadInt32();

    public int Arg1 { get; init; } = reader.ReadInt32();

    public int Arg2 { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Arg0);
        writer.Write(Arg1);
        writer.Write(Arg2);
    }
}
