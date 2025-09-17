using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Requests;

[HotspotMessage(Group, Command)]
public readonly struct QuestEventUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.EventUpdate;

    public int Episode { get; } = reader.ReadInt32();
    public int Condition { get; } = reader.ReadInt32();
    public long Param { get; } = reader.ReadInt64();

    public MessageOpcode Opcode => new(Group, Command);
}