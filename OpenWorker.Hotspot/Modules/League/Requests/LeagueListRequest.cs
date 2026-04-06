using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.League.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.List;

    public LeagueListType Type { get; } = reader.ReadLeagueListType();
    
    public MessageOpcode Opcode => new(Group, Command);
}