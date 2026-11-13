using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Requests;

/// <summary>
/// A player uses a social object it stands by: the object, and the player's own place and facing.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct SocialItemUseRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Use;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Prop { get; }

    public Vector3 Position { get; }

    public float Rotation { get; }
}
