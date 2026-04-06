using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ChatNormalResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Normal;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorComponent Actor { get; init; }
    public ChatMessageAppearance Appearance { get; init; }
    public required string Message { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Appearance);
        writer.WriteChatMessage(Message);
    }

#endregion Interface: IWritableData
}
