using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Drops;

/// <summary>
/// One thing lying on the ground: an item or money, by the id of the object, and where.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct DropObjectValue(Vector3 Position, int ObjectId, short Count, short Bonus, long Serial);

/// <summary>
/// Things that fell from a monster. The group is told again when they are taken away all at once.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct DropInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Drop;
    private const DropOpcode Command = DropOpcode.Infos;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int DropGroup { get; init; }
    public bool Boss { get; init; }
    [WireArray(WireLength.Byte)]
    public required IReadOnlyList<DropObjectValue> Objects { get; init; }

    /// <summary>
    /// The object that is shown as a shower of money when it falls, and how many times over.
    /// </summary>
    public int ShowerObject { get; init; }
    public byte ShowerMultiple { get; init; }
}
