using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Sets what each position may do.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LeagueAuthChangeRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChange;

    [WireArray(Count = LeagueModuleDefines.LeagueRewardCount)]
    public IReadOnlyList<LeagueRewardInfo> Rewards { get; }

    public int Result { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
