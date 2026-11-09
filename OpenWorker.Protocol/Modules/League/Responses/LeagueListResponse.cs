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
/// The leagues of a page of the list, and their masters.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.List;

    public IReadOnlyList<LeagueInfo> Leagues { get; init; } = [];
    public IReadOnlyList<int> Actors { get; init; } = [];

    public LeagueListResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
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
