using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

/// <summary>
/// Sets what each position may do.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueAuthChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChange;

    public IReadOnlyList<LeagueRewardInfo> Rewards { get; } = Enumerable.Range(0, LeagueModuleDefines.LeagueRewardCount).Select(_ => new LeagueRewardInfo(reader)).ToArray();
    public int Result { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
