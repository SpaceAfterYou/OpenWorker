using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct WorldEnterRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.EnterReq;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Account { get; }
    public ActorValue Actor { get; }
    public int Party { get; }
    public short Location { get; }
    public short Channel { get; }
    public int Jump { get; }
    public int Portal { get; }
    public MapValue Map { get; }
    public short DistrictWorld { get; }
    public ChangeServerType ChangeType { get; }
}
