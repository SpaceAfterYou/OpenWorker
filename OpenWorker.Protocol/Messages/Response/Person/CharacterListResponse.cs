using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct CharacterListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ListRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required IReadOnlyList<PersonValue> Persons { get; init; }
    public int LastPerson { get; init; }
    public required ProtectionStateValue Protection { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Persons.Count);

        foreach (var person in Persons)
        {
            writer.Write(person);
        }

        writer.Write(LastPerson);
        writer.Write(Protection);
    }
}
