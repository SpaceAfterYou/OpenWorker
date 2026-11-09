using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

/// <summary>
/// Legacy client handler only logs; no payload consumed.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct QuestEventUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.EventUpdate;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage
}
