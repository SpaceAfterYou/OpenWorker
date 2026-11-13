using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Requests;

/// <summary>
/// A player puts a social object down with an item of its bag: the slot and serial of the item, the item, the row of the
/// social item table, and the place in front of the player the client picked with the way the object faces.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct SocialItemStartRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Start;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public StorageGroup Storage { get; }

    public short Slot { get; }

    public long Serial { get; }

    public int Item { get; }

    public short SocialItem { get; }

    public Vector3 Position { get; }

    public float Rotation { get; }
}
