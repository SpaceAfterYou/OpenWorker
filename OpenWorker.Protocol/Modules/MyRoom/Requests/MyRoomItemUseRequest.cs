using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Requests;

/// <summary>
/// A player sits or lies on a piece: where the player and the piece stand and face, and the animation the client picked.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MyRoomItemUseRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.ItemUse;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; }

    public long Serial { get; }

    public Vector3 Position { get; }

    public float Rotation { get; }

    public Vector3 FurniturePosition { get; }

    public float FurnitureRotation { get; }

    public byte Animation { get; }
}
