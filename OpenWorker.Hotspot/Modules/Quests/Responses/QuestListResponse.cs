using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Quests.Types;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

[HotspotMessage(Group, Command)]
public readonly struct QuestListResponse(IReadOnlyList<QuestEpisodeEntry> list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.List;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write((short)list.Count);

        foreach (var quest in list)
        {
            writer.Write(quest.Index);

            writer.Write(quest.Info.AddHelper);
            writer.Write(quest.Info.CompleteBit);
            writer.Write(quest.Info.Failed);

            Debug.Assert(quest.Info.Condition.Count <= 10);

            foreach (var condition in quest.Info.Condition)
            {
                writer.Write(condition.Condition);
                writer.Write(condition.Step);
            }
        }
    }
}