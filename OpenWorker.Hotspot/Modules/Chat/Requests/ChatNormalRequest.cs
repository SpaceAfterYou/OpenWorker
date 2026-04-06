using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ChatNormalRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Normal;

    public ChatMessageAppearance Appearance { get; } = reader.ReadChatMessageAppearance();
    public string Message { get; } = reader.ReadChatMessage();

    public MessageOpcode Opcode => new(Group, Command);
}