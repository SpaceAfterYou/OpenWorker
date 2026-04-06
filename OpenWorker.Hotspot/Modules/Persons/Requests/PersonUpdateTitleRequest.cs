using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PersonUpdateTitleRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateTitle;

    /// <summary>
    /// Apply ability
    /// </summary>
    public TitleValue Ability { get; } = new(reader);
    
    /// <summary>
    /// Just show names
    /// </summary>
    public TitleValue Represent { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}