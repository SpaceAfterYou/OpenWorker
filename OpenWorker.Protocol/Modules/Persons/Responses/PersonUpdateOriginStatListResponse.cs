using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The base stats of a player for the status window. Each stat goes value first, then the stat in a short, unlike the
/// monster info of the world, so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonUpdateOriginStatListResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateOriginStat;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ActorValue Actor { get; init; }

    /// <summary>
    /// The stats the status window shows, the ones left out are zero.
    /// </summary>
    public required StatKeyValuePair[] Stats { get; init; }

    public void Write(ref PacketWriter writer)
    {
        Actor.Write(ref writer);
        StatKeyValuePair.WriteValueFirst(ref writer, Stats);
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
