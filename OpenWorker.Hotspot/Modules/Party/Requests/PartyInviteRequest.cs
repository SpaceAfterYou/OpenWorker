using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PartyInviteRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Invite;

    public MessageOpcode Opcode => new(Group, Command);

    public string TargetName { get; } = reader.ReadPersonName();
    public string RequesterName { get; } = reader.ReadPersonName();
    public ActorValue RequesterActor { get; } = new(reader);
    public ActorValue TargetActor { get; } = new(reader);
    public int Server { get; } = reader.ReadInt32();
    public int Result { get; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.WritePersonName(TargetName);
        writer.WritePersonName(RequesterName);
        writer.Write(RequesterActor);
        writer.Write(TargetActor);
        writer.Write(Server);
        writer.Write(Result);
    }
}
