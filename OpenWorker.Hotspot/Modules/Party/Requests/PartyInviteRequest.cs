using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command)]
public readonly struct PartyInviteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Invite;
    
    public string MasterName { get; } = reader.ReadPersonName();
    public string RequestName { get; } = reader.ReadPersonName();
    public ActorValue MasterActor { get; } = new(reader);
    public ActorValue RequestActor { get; } = new(reader);
    public int Server { get; } = reader.ReadInt32();
    public int Result { get; } = reader.ReadInt32();
    public int Unknown { get; } = reader.ReadByte();
    
    public MessageOpcode Opcode => new(Group, Command);
}