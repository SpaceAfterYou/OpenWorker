using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// What a player sees about another one.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonOtherInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.OtherInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public bool CalculateStat { get; init; }
    public OtherPersonValue Target { get; init; }
    public IReadOnlyList<ushort> Categories { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(CalculateStat);
        Target.Write(writer);

        foreach (var category in Categories)
        {
            writer.Write(category);
        }
    }

#endregion Interface: IWritableData
}
