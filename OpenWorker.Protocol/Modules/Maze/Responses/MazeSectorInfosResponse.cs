using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazeSectorInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_SECTOR_INFOS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public IReadOnlyCollection<int> Cleared { get; init; }
    public int Current { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Cleared.Count);
        foreach (var item in Cleared)
        {
            writer.Write(item);
        }
        writer.Write(Current);
    }
}
