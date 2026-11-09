using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazePortalInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_POTAL_INFOS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public IReadOnlyCollection<int> Open { get; init; }
    public IReadOnlyCollection<int> Closed { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Open.Count);
        writer.Write(Closed.Count);
        foreach (var item in Open)
        {
            writer.Write(item);
        }
        foreach (var item in Closed)
        {
            writer.Write(item);
        }
    }
}
