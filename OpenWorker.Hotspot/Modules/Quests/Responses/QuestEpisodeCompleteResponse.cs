using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Quests.Types;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct QuestEpisodeCompleteResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.EpisodeComplete;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Episode { get; init; } = reader.ReadInt32();
    public int Exp { get; init; }  = reader.ReadInt32();
    public int Money { get; init; }  = reader.ReadInt32();
    public int BattlePoints { get; init; }  = reader.ReadInt32();
    public int Ether { get; init; }  = reader.ReadInt32();
    public TitleValue Title { get; init; } = new(reader);
    public uint NpcHelper { get; init; } = reader.ReadUInt32();
    public CreateItemValue Item { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Episode);
        writer.Write(Exp);
        writer.Write(Money);
        writer.Write(BattlePoints);
        writer.Write(Ether);
        writer.Write(Title);
        writer.Write(NpcHelper);
        
        Item.Write(writer);
    }

#endregion Interface: IWritableData
}
