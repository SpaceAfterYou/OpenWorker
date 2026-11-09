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
/// The leagues that were found.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueSearchResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Search;

    public byte State { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Master { get; init; } = string.Empty;
    public int Reserved { get; init; }
    public IReadOnlyList<LeagueInfo> Leagues { get; init; } = [];
    public IReadOnlyList<int> Actors { get; init; } = [];

    public LeagueSearchResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(State);
        writer.WriteLeagueName(Name);
        writer.WritePersonName(Master);
        writer.Write(Reserved);
        writer.Write(checked((byte)Leagues.Count));
        foreach (var value in Leagues)
        {
            writer.Write(value);
        }
        writer.Write(checked((byte)Actors.Count));
        foreach (var value in Actors)
        {
            writer.Write(value);
        }
    }
}
