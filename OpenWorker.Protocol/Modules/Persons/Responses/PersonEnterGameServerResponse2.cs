using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The answer of a district or a maze to a client that came from another server of the game: the play scene puts the player
/// at the place, loads the map and asks for the objects around it. A client that came from the character selection reads the
/// character record (03/32) instead. The gate has the same packet with a different structure.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonEnterGameServerResponse2 : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterGameServerRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public WorldValue World { get; init; }

    public byte EchelonLevel { get; init; }

    public int EchelonExperience { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(0); // error code
        writer.Write((byte)0); // unknown
        writer.Write(World);
        writer.Write(EchelonLevel);
        writer.Write(EchelonExperience);
    }

#endregion Interface: IWritableData
}
