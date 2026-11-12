using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// How each member of the group did, in the order of the results window.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MazeRewardPartyMember(byte Order, byte Rank, int Member, [property: WireString(WireStringFormat.Utf16Unicode)] string Name, int PlayPoint);

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeRewardPartyInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_REWARD_PARTY_INFO;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public required IReadOnlyList<MazeRewardPartyMember> Members { get; init; }
}
