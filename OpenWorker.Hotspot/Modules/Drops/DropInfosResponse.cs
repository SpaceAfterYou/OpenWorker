using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Drops;

/// <summary>
/// One thing lying on the ground: an item or money, by the id of the object, and where.
/// </summary>
public readonly record struct DropObjectValue(Vector3 Position, int Object, short Count, short Bonus, long Serial);

/// <summary>
/// Things that fell from a monster. The group is told again when they are taken away all at once.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct DropInfosResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Drop;
    private const DropOpcode Command = DropOpcode.Infos;

    public MessageOpcode Opcode => new(Group, Command);

    public required int DropGroup { get; init; }
    public bool Boss { get; init; }
    public required IReadOnlyList<DropObjectValue> Objects { get; init; }

    /// <summary>
    /// The object that is shown as a shower of money when it falls, and how many times over.
    /// </summary>
    public int ShowerObject { get; init; }
    public byte ShowerMultiple { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(DropGroup);
        writer.Write(Boss);
        writer.Write(checked((byte)Objects.Count));

        foreach (var value in Objects)
        {
            writer.Write(value.Position.X);
            writer.Write(value.Position.Y);
            writer.Write(value.Position.Z);
            writer.Write(value.Object);
            writer.Write(value.Count);
            writer.Write(value.Bonus);
            writer.Write(value.Serial);
        }

        writer.Write(ShowerObject);
        writer.Write(ShowerMultiple);
    }
}
