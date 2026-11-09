using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PartyInviteResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Invite;

    public string TargetName { get; init; } = reader.ReadPersonName();
    public string RequesterName { get; init; } = reader.ReadPersonName();
    public ActorValue RequesterActor { get; init; } = new(reader);
    public ActorValue TargetActor { get; init; } = new(reader);
    public int Server { get; init; } = reader.ReadInt32();
    public int Result { get; init; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, (byte)Command);

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
