using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ChatMegaphoneRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Megaphone;

    public UseItemFrom Item { get; }
    public int Person { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Message { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}