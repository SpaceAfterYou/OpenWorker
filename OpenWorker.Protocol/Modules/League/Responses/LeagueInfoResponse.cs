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
/// Everything about the league of the player. Joined makes the client say so.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Info;

    public required LeagueInfo Info { get; init; }
    public IReadOnlyList<LeagueMember> Members { get; init; } = [];
    public IReadOnlyList<LeagueApplicant> Applicants { get; init; } = [];
    public IReadOnlyList<LeagueBoardEntry> Board { get; init; } = [];
    public bool Joined { get; init; }

    public LeagueInfoResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Info);
        writer.Write(checked((byte)Members.Count));
        foreach (var value in Members)
        {
            writer.Write(value);
        }
        writer.Write(checked((byte)Applicants.Count));
        foreach (var value in Applicants)
        {
            writer.Write(value);
        }
        writer.Write(checked((byte)Board.Count));
        foreach (var value in Board)
        {
            writer.Write(value);
        }
        writer.Write(Joined);
    }
}
