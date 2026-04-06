using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.System.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SystemXignCodeRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.Xigncode;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Session { get; } = reader.ReadInt32();
    public Memory<byte> Buffer { get; } = reader.ReadBytes(reader.ReadInt16());

#endregion Message: Body
}
