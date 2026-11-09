using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Types;

namespace OpenWorker.Hotspot.Modules.System.Requests;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct SystemXignCodeRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.Xigncode;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Session { get; } = reader.ReadInt32();
    public Memory<byte> Buffer { get; } = reader.ReadBytes(SystemModuleDefines.XignCodeBufferSize);
    public string Text { get; } = reader.ReadUtf8AsciiStringWithoutTerminator();

#endregion Message: Body
}
