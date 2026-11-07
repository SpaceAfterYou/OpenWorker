using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// Achievements that have moved on.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonAchieveUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.AchieveUpdate;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyCollection<AchieveUpdateValue> Updates { get; init; }
    public byte[] RewardBits { get; init; }
    public IReadOnlyList<ushort> Categories { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Updates.Count);

        foreach (var update in Updates)
        {
            update.Write(writer);
        }

        writer.Write(RewardBits);

        foreach (var category in Categories)
        {
            writer.Write(category);
        }
    }

#endregion Interface: IWritableData
}
