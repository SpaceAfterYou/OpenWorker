using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

/// <summary>
/// How often the player has entered each maze today, sent as the player enters the world from the gate: the client replaces
/// the counts it has with these. The client counts the list in a byte and reads each entry as an actor, a maze and a count.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct WorldEnterMazeLimitCountLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.EnterMazeLimitCountLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<MazeLimitCountValue> Counts { get; init; }

#endregion Message: Body
}

/// <summary>
/// The count of one maze in a <see cref="WorldEnterMazeLimitCountLoadResponse"/>.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct MazeLimitCountValue
{
    public ActorValue Actor { get; init; }

    public ushort Maze { get; init; }

    public ushort Count { get; init; }
}
