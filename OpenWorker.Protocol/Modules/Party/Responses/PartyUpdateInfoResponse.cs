using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyUpdateInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateUserInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Party { get; init; }
    public ActorValue Master { get; init; }
    public MapValue Maze { get; init; }
    public PartyUpdateType UpdateType { get; init; }
    public PartyType Type { get; init; }
    [WireArray(WireLength.Byte)]
    public PartyMemberInfoValue[] InfoList { get; init; }
}
