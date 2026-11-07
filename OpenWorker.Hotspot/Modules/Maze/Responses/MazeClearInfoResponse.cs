using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Maze.Types;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The mazes a person has cleared. The client puts them next to the ones it knows, or throws the old ones away.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeClearInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_CLEAR_INFO;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyList<MazeClearValue> Groups { get; init; }
    public ActorValue Person { get; init; }

    /// <summary>
    /// True when the list is all the person has, and what the client knew before is forgotten.
    /// </summary>
    public bool IsReset { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Groups.Count);

        foreach (var group in Groups)
        {
            writer.Write(group.Group);
            writer.Write(group.OpenEpisode);

            // The numbers go in pairs, one slot after another.
            for (var slot = 0; slot < MazeClearValue.Slots; slot++)
            {
                var (maze, value) = slot < group.Cleared.Count ? group.Cleared[slot] : default;

                writer.Write(maze);
                writer.Write(value);
            }
        }

        writer.WriteActor(Person);
        writer.Write(IsReset);
    }

#endregion Interface: IWritableData
}
