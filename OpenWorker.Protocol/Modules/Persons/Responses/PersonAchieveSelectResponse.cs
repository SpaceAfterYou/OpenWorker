using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// Achievements of the player.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonAchieveSelectResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.AchieveSelect;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    [WireArray(Count = PersonModuleDefines.AchievementRewardBytes)]
    public byte[] RewardBits { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<AchieveValue> Achievements { get; init; }

    [WireArray(Count = PersonModuleDefines.AchievementCategories)]
    public IReadOnlyList<ushort> Categories { get; init; }

#endregion Message: Body
}
