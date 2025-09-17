using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Types;

namespace OpenWorker.Hotspot.Modules.Chat.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ChatMegaphoneRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Megaphone;

    public UseItemFrom Item { get; } = new(reader);
    public int Person { get; } = reader.ReadInt32();
    public string Name { get; } = reader.ReadUtf8UnicodeString();
    public string Message { get; } = reader.ReadUtf8UnicodeString();

    public MessageOpcode Opcode => new(Group, Command);
}