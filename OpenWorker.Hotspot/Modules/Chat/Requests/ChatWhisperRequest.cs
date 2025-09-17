using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Chat.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ChatWhisperRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Whisper;

    public string Sender { get; } = reader.ReadUtf8UnicodeString();
    public string Receiver { get; } = reader.ReadUtf8UnicodeString();
    public string Message { get; } = reader.ReadUtf8UnicodeString();
    public int Result { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}