using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Types;

public readonly struct PartyApplyMemberValue(BinaryReader reader) : IWritableData
{
    public PartyMemberInfoValue Member { get; init; } = new(reader);
    public int RegDate { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        Member.Write(writer);

        writer.Write(RegDate);
    }
}
