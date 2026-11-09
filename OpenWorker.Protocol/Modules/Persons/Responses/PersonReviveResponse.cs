using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// A player comes back to life at a place. The client reads this one where the revive broadcast is
/// registered, not at the code that stands before it in the list.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonReviveResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ReviveBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Actor { get; init; }
    public int Owner { get; init; }
    public byte Type { get; init; }
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }

#endregion Message: Body
}
