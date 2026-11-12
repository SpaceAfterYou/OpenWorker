using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The call for new members. The time to wait goes only with the result that says to wait, so the writer is written by
/// hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueRecruitNoticeResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.RecruitNotice;

    public int League { get; init; }
    public string Notice { get; init; } = string.Empty;
    public long Date { get; init; }
    public int Result { get; init; }
    public long Remain { get; init; }

    public LeagueRecruitNoticeResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(League);
        writer.WriteUtf16UnicodeString(Notice);
        writer.WriteInt64(Date);
        writer.WriteInt32(Result);

        // Only an answer that says to wait has the time to wait.
        if (Result == 57029)
        {
            writer.WriteInt64(Remain);
        }
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
