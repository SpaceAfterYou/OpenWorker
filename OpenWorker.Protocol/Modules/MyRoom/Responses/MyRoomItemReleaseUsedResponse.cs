using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Responses;

/// <summary>
/// A player got up from a piece and stands where it stood before.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MyRoomItemReleaseUsedResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.ItemReleaseUsed;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; init; }

    public long Serial { get; init; }

    public Vector3 Position { get; init; }

    public float Rotation { get; init; }
}
