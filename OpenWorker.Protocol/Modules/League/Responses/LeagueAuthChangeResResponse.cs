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
/// The answer to changing what the positions may do.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueAuthChangeResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChangeRes;

    public IReadOnlyList<LeagueRewardInfo> Rewards { get; init; } = [..Enumerable.Repeat(new LeagueRewardInfo(), 9)];
    public int Result { get; init; }

    public LeagueAuthChangeResResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, Command);

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
