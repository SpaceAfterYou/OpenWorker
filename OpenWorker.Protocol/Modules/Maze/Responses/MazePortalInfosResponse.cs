using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The portals of the maze that are open and closed. Both counts go first, then both lists, so the writer is written by
/// hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct MazePortalInfosResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_POTAL_INFOS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public IReadOnlyCollection<int> Open { get; init; }
    public IReadOnlyCollection<int> Closed { get; init; }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteLength(WireLength.Signed32, Open?.Count ?? 0);
        writer.WriteLength(WireLength.Signed32, Closed?.Count ?? 0);

        foreach (var item in Open ?? [])
        {
            writer.WriteInt32(item);
        }

        foreach (var item in Closed ?? [])
        {
            writer.WriteInt32(item);
        }
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
