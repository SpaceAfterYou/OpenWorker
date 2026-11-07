using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonGobackLobbyResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.GobackLobby;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue Actor { get; init; }
    public required int Account { get; init; }
    public required string Address { get; init; }
    public required short Port { get; init; }
    public required LeaveDestination Destination { get; init; }
    public required bool Result { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Account);
        writer.WriteUtf8AsciiStringWithoutTerminator(Address);
        writer.Write(Port);
        writer.Write((byte)Destination);
        writer.Write(Result);
    }
}
