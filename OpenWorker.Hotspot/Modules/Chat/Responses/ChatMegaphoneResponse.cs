using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[HotspotMessage(Group, Command)]
public readonly struct ChatMegaphoneResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Megaphone;
    
    public required StorageGroup Storage { get; init; }
    public required short Slot { get; init; }
    public required int Person { get; init; }
    public required string Name { get; init; }
    public required string Message { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Storage);
        writer.Write(Slot);
        writer.Write(Person);
        writer.WritePersonName(Name);
        writer.WriteChatMessage(Message);
    }
}