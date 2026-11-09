using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The share points of every group of mazes the person has cleared a maze of, sent after the districts: the client files a
/// group under its district and drops one whose district it does not know. Taken only while the client enters the world.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonLoadMazeStateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.LoadMazeState;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyCollection<MazeStateValue> States { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)States.Count);

        foreach (var state in States)
        {
            state.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
