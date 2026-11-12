using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.System.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct SystemXignCodeRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.Xigncode;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Session { get; }

    [WireArray(Count = SystemModuleDefines.XignCodeBufferSize)]
    public byte[] Buffer { get; }

    [WireString(WireStringFormat.Utf8AsciiWithoutTerminator)]
    public string Text { get; }

#endregion Message: Body
}
