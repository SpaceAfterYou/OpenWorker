using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[HotspotMessage(Group, Command)]
public readonly struct ChatWhisperResponse(string sender, string receiver, string message) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Whisper;

    public MessageOpcode Opcode => new(Group, Command);

    /// <summary>
    ///  TODO: or bool? or has not?
    /// </summary>
    private int HasError { get; init; }

    public void ToBinary(BinaryWriter writer)
    {
        writer.WritePersonName(sender);
        writer.WritePersonName(receiver);
        writer.WriteChatMessage(message);

        writer.Write(HasError);
    }

    public static ChatWhisperResponse Error => new(string.Empty, string.Empty, string.Empty) { HasError = 1 };
}