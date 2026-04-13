using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Types;

public readonly struct PartyRecruitMemberValue(BinaryReader reader) : IWritableData
{
    public int Party { get; init; } = reader.ReadInt32();
    public string Message { get; init; } = reader.ReadUtf8UnicodeString(21);
    public short MinLevel { get; init; } = reader.ReadInt16();
    public short MaxLevel { get; init; } = reader.ReadInt16();
    public byte Purpose { get; init; } = reader.ReadByte();
    public string MasterName { get; init; } = reader.ReadPersonName();
    public byte MemberCount { get; init; } = reader.ReadByte();
    public int RemainTime { get; init; } = reader.ReadInt32();
    public int PurposeLocation { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Party);
        writer.WriteUtf16UnicodeString(Message, PartyModuleDefines.RecruitMessageLength);
        writer.Write(MinLevel);
        writer.Write(MaxLevel);
        writer.Write(Purpose);
        writer.WritePersonName(MasterName);
        writer.Write(MemberCount);
        writer.Write(RemainTime);
        writer.Write(PurposeLocation);
    }
}
