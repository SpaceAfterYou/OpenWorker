using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Gestures.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct GestureShowRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.Show;

    public int Identifier { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}