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

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// What the positions may do changed.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueAuthChangeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChange;

    public IReadOnlyList<LeagueRewardInfo> Rewards { get; init; } = [..Enumerable.Repeat(new LeagueRewardInfo(), 9)];
    public int Result { get; init; }

    public LeagueAuthChangeResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Rewards.Count is LeagueModuleDefines.LeagueRewardCount);
        foreach (var reward in Rewards)
        {
            writer.Write(reward);
        }
        writer.Write(Result);
    }
}
