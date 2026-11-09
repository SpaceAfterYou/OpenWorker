using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazeGateInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_GATE_INFOS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public bool ReEnter { get; init; }
    public IReadOnlyCollection<int> Open { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ReEnter);
        writer.Write(Open.Count);
        foreach (var item in Open)
        {
            writer.Write(item);
        }
    }
}
