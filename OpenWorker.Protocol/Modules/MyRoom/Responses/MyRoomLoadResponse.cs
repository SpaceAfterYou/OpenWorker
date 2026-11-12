using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Responses;

/// <summary>
/// The furniture of the room the player has come into; a byte tells how many.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MyRoomLoadResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.Load;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public required IReadOnlyList<MyRoomFurnitureValue> Furniture { get; init; }
}
