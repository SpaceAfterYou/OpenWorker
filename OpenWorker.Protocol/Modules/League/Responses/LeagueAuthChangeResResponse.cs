using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to changing what the positions may do.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueAuthChangeResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChangeRes;

    public LeagueAuthChangeResResponse()
    {
    }

    [WireArray(Count = LeagueModuleDefines.LeagueRewardCount)]
    public IReadOnlyList<LeagueRewardInfo> Rewards { get; init; } = [..Enumerable.Repeat(new LeagueRewardInfo(), LeagueModuleDefines.LeagueRewardCount)];

    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
