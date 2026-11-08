using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Party.Extensions;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyAwaiterAddResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterAdd;

    public MessageOpcode Opcode => new(Group, Command);

    public PartyAwaiterValue Awaiter { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        Awaiter.Write(writer);
    }
}

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyAwaiterDelResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterDel;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
    }
}

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyAwaiterListResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterList;

    public MessageOpcode Opcode => new(Group, Command);

    public PartyAwaiterValue[] List { get; init; } = reader.ReadPartyAwaiters();

    public void Write(BinaryWriter writer)
    {
        writer.Write(List);
    }
}

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyUnknown5Response : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Unknown5;

    public MessageOpcode Opcode => new(Group, Command);
}
