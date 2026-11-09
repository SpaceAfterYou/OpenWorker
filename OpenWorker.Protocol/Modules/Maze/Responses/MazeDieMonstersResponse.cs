using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazeDieMonstersResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_DIE_MONSTERS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public IReadOnlyCollection<int> SpawnBoxes { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(SpawnBoxes.Count);
        foreach (var item in SpawnBoxes)
        {
            writer.Write(item);
        }
    }
}
