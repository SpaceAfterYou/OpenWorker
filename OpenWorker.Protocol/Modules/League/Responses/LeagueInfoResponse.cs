using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// Everything about the league of the player. Joined makes the client say so.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Info;

    public LeagueInfoResponse()
    {
    }

    public required LeagueInfo Info { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueMember> Members { get; init; } = [];

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueApplicant> Applicants { get; init; } = [];

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueBoardEntry> Board { get; init; } = [];

    public bool Joined { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
