using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly partial struct ChatWhisperResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Whisper;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Sender { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Receiver { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Message { get; init; }

    /// <summary>
    /// Zero for a whisper that arrived. Anything else is the number of the text the client shows the sender in place of it.
    /// </summary>
    public int Result { get; init; }

#endregion Message: Body
}
