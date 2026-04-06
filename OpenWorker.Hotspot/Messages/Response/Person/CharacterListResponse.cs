using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct CharacterListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ListRes;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<PersonValue> Persons { get; init; }
    public int LastIndex { get; init; }
    public required ProtectionStateValue Protection { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Persons.Count);

        foreach (var person in Persons)
        {
            writer.Write(person);
        }

        writer.Write(LastIndex);
        writer.Write(Protection);
    }
}
