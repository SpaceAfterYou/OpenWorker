using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazeInteractionInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_INTERACTION_INFOS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public IReadOnlyList<MazeInteractionValue> Interactions { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Interactions.Count);
        foreach (var item in Interactions)
        {
            writer.Write(item.Show);
            writer.Write(item.Enable);
            writer.Write(item.Box);
            writer.Write(item.CallCount);
        }
    }
}
