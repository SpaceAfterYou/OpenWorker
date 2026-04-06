using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ChatWhisperResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Whisper;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    ///  TODO: or bool? or has not?
    /// </summary>
    public int HasError { get; init; }

    public required string Sender { get; init; }
    public required string Receiver { get; init; }
    public required string Message { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WritePersonName(Sender);
        writer.WritePersonName(Receiver);
        writer.WriteChatMessage(Message);

        writer.Write(HasError);
    }

#endregion Interface: IWritableData

    public static ChatWhisperResponse Error => new()
    {
        Sender = string.Empty,
        Receiver = string.Empty,
        Message = string.Empty,
        HasError = 1
    };
}
