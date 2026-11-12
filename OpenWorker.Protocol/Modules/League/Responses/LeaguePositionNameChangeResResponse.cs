using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to renaming a position.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeaguePositionNameChangeResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.PositionNameChangeRes;

    public LeaguePositionNameChangeResResponse()
    {
    }

    public int Position { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; } = string.Empty;

    public int Result { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string PreviousName { get; init; } = string.Empty;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
