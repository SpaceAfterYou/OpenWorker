using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command)]
public readonly struct PersonEnterGameServerRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterGameServerReq;

    public int Account { get; } = reader.ReadInt32();
    public ActorValue Actor { get; } = new(reader);
    public MapValue Map { get; } = reader.ReadMapValue();
    public bool FirstConnect { get; } = reader.ReadBoolean();
    public SessionValue Session { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}