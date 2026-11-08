using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly partial struct ChatMegaphoneResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Megaphone;
    
    public required StorageGroup Storage { get; init; }
    public required short Slot { get; init; }
    public required int Person { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Message { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);
}