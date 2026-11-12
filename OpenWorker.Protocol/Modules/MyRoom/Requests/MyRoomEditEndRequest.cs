using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Requests;

/// <summary>
/// The owner is done arranging: the pieces that moved or turned, where they stand now.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MyRoomEditEndRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.EditEnd;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<MyRoomFurnitureValue> Pieces { get; }
}
