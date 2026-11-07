using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// How each member of the group did, in the order of the results window.
/// </summary>
public readonly record struct MazeRewardPartyMember(byte Order, byte Rank, int Member, string Name, int PlayPoint);

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeRewardPartyInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_REWARD_PARTY_INFO;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<MazeRewardPartyMember> Members { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(checked((byte)Members.Count));

        foreach (var member in Members)
        {
            writer.Write(member.Order);
            writer.Write(member.Rank);
            writer.Write(member.Member);
            writer.WritePersonName(member.Name);
            writer.Write(member.PlayPoint);
        }
    }
}
