using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.League.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueRecruitNoticeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.RecruitNotice;

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
    }
}