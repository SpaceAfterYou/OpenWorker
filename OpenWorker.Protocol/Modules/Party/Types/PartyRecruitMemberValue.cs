using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Types;

/// <summary>
/// A party on the recruit board. The client sends it when it puts a party up and reads it in the board, and the two
/// count the length of a text differently: in bytes when the client sends it, in characters when the server does. So the
/// reader and the writer are written by hand, with the field order of the properties.
/// </summary>
public readonly struct PartyRecruitMemberValue : IWireReadable<PartyRecruitMemberValue>, IWireWritable
{
    public int Party { get; init; }
    public string Message { get; init; }
    public short MinLevel { get; init; }
    public short MaxLevel { get; init; }
    public byte Purpose { get; init; }
    public string MasterName { get; init; }
    public byte MemberCount { get; init; }
    public int RemainTime { get; init; }
    public int PurposeLocation { get; init; }

    public static PartyRecruitMemberValue Read(ref PacketReader reader) => new()
    {
        Party = reader.ReadInt32(),
        Message = reader.ReadUtf8UnicodeString(),
        MinLevel = reader.ReadInt16(),
        MaxLevel = reader.ReadInt16(),
        Purpose = reader.ReadByte(),
        MasterName = reader.ReadUtf8UnicodeString(),
        MemberCount = reader.ReadByte(),
        RemainTime = reader.ReadInt32(),
        PurposeLocation = reader.ReadInt32()
    };

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Party);
        writer.WriteUtf16UnicodeString(Message);
        writer.WriteInt16(MinLevel);
        writer.WriteInt16(MaxLevel);
        writer.WriteByte(Purpose);
        writer.WriteUtf16UnicodeString(MasterName);
        writer.WriteByte(MemberCount);
        writer.WriteInt32(RemainTime);
        writer.WriteInt32(PurposeLocation);
    }
}
