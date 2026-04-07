using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Quests.Types;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

/// <summary>
/// int16 count, then count × (int32 condition id, byte step) — sub_417690 / receive_eSUB_CMD_QUEST_UPDATE.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct QuestUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.Update;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyList<QuestCondition> ConditionList { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)ConditionList.Count);

        foreach (var value in ConditionList)
        {
            writer.Write(value.Condition);
            writer.Write(value.Step);
        }
    }

#endregion Interface: IWritableData
}
