using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The answer of the character selection for a character that has not played the prologue: the zone is the first
/// tutorial maze. The client handles it like the ordinary answer (<c>03/14</c>): on a zero result it connects to the
/// address of the zone and enters the game server, otherwise it shows the message of the code.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonEnterPrologueResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterPrologue;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required EnterMapResultValue Zone { get; init; }
}
