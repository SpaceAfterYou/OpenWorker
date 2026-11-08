using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

/// <summary>
/// A line across the top of the screen. The colour is six hex digits.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ChatNoticeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Notice;

    public const string DefaultColor = "FF0000";

    /// <summary>
    /// How the client shows it; it only uses it to pick the place of the line.
    /// </summary>
    public byte Type { get; init; }

    public required string Message { get; init; }
    public string Color { get; init; }

    public ChatNoticeResponse()
    {
        Color = DefaultColor;
    }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Type);
        writer.WriteChatMessage(Message);
        writer.WriteChatColorMessage(Color);
    }
}
