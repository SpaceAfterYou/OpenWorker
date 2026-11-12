using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

/// <summary>
/// The places of the deck a player saves: a count, then the places. At most <see cref="SkillModuleDefines.SkillDeckCount"/>
/// places are read, whatever the count says, and what follows them is left; a list cut to a limit is no declared format, so
/// the reader is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct SkillUpdateDeckRequest : IRequestHotspotMessage, IWireReadable<SkillUpdateDeckRequest>
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillUpdateDeck;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

    public SkillUpdateDeckRequest(BinaryReader reader)
    {
        this = WireBridge.Read<SkillUpdateDeckRequest>(reader);
    }

#region Message: Body

    public SkillDeckValue[] SlotList { get; init; }

#endregion Message: Body

    public static SkillUpdateDeckRequest Read(ref PacketReader reader)
    {
        var slots = new SkillDeckValue[Math.Min(reader.ReadByte(), SkillModuleDefines.SkillDeckCount)];

        for (var i = 0; i < slots.Length; i++)
        {
            slots[i] = SkillDeckValue.Read(ref reader);
        }

        return new SkillUpdateDeckRequest { SlotList = slots };
    }
}
