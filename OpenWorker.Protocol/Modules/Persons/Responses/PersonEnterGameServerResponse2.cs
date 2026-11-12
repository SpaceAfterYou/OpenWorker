using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The answer of a district or a maze to a client that came from another server of the game: the play scene puts the player
/// at the place, loads the map and asks for the objects around it. A client that came from the character selection reads the
/// character record (03/32) instead. The gate has the same packet with a different structure.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonEnterGameServerResponse2 : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterGameServerRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// The error code: always zero, this answer goes only to a client that entered.
    /// </summary>
    public int Result { get; init; }

    /// <summary>
    /// Always zero; its meaning is not known.
    /// </summary>
    public byte Unknown { get; init; }

    public WorldValue World { get; init; }

    public byte EchelonLevel { get; init; }

    public int EchelonExperience { get; init; }

#endregion Message: Body
}
