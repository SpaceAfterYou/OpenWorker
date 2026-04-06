using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct CharacterInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.InfoRes;

    public MessageOpcode Opcode => new(Group, Command);

    public required PersonValue Person { get; init; }
    public required WorldValue World { get; init; }
    public required CharacterInfoGatePayload Gate { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Person);
        writer.Write(World);
        writer.Write(Gate);
    }
}
