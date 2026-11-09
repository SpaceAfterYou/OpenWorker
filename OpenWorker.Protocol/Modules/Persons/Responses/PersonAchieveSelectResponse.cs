using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// Achievements of the player.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonAchieveSelectResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.AchieveSelect;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte[] RewardBits { get; init; }
    public IReadOnlyCollection<AchieveValue> Achievements { get; init; }
    public IReadOnlyList<ushort> Categories { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(RewardBits);
        writer.Write((byte)Achievements.Count);

        foreach (var achievement in Achievements)
        {
            achievement.Write(writer);
        }

        foreach (var category in Categories)
        {
            writer.Write(category);
        }
    }

#endregion Interface: IWritableData
}
