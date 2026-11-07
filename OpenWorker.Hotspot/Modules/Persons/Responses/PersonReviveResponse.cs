using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using System.Numerics;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// A player comes back to life at a place.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonReviveResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ReviveRes;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Actor { get; init; }
    public int Owner { get; init; }
    public byte Type { get; init; }
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Owner);
        writer.Write(Type);
        writer.Write(Position);
        writer.Write(Rotation);
    }

#endregion Interface: IWritableData
}
