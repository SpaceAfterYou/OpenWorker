using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct QuestHelperRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.Helper;

    public QuestHelperType Type { get; }
    public int Episode { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}