using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct ChatWhisperRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Whisper;

    [WireString(WireStringFormat.Utf8Unicode)]
    public string Sender { get; }
    [WireString(WireStringFormat.Utf8Unicode)]
    public string Receiver { get; }
    [WireString(WireStringFormat.Utf8Unicode)]
    public string Message { get; }
    public int Result { get; }

    public MessageOpcode Opcode => new(Group, Command);
}