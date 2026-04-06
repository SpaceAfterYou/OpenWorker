using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct QuestEpisodeCompleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.EpisodeComplete;

    public int Episode { get; } = reader.ReadInt32();
    public int RewardItem { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}