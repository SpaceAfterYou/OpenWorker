using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Requests;

/// <summary>
/// The owner names the room and says whether others may come in (0 open, 1 closed).
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MyRoomSetupRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.Setup;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public byte OpenLevel { get; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }
}
