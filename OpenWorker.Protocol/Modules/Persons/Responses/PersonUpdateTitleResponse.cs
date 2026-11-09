using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The titles a player has chosen. When it failed, the client keeps the old ones.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonUpdateTitleResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateTitle;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public TitleValue Ability { get; init; }
    public TitleValue Represent { get; init; }
    public bool Result { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Ability.Value);
        writer.Write(Represent.Value);
        writer.Write(Result);
    }

#endregion Interface: IWritableData
}
