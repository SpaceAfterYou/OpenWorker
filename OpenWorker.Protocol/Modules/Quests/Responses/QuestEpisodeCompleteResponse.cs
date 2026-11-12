using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct QuestEpisodeCompleteResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.EpisodeComplete;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Episode { get; init; }
    public int Exp { get; init; }
    public int Money { get; init; }
    public int BattlePoints { get; init; }
    public int Ether { get; init; }
    public TitleValue Title { get; init; }
    public uint NpcHelper { get; init; }

    [WireArray(WireLength.Byte)]
    public CreateItemValue[] Items { get; init; }

#endregion Message: Body
}
