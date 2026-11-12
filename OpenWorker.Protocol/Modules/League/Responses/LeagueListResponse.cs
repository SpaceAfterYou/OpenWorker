using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The leagues of a page of the list, and their masters.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.List;

    public LeagueListResponse()
    {
    }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<LeagueInfo> Leagues { get; init; } = [];

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<int> Actors { get; init; } = [];

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
