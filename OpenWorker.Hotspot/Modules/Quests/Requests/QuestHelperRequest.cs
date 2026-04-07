using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Extensions;

namespace OpenWorker.Hotspot.Modules.Quests.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct QuestHelperRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.Helper;

    public QuestHelperType Type { get; } = reader.ReadQuestHelperType();
    public int Episode { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}