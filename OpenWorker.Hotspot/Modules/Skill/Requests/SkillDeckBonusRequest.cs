using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Extensions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SkillDeckBonusRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillDeckBonus;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ushort[] Deck { get; init; } = reader.ReadSkillDeckBonus();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteSkillDeckBonus(Deck);
    }

#endregion Interface: IWritableData
}
