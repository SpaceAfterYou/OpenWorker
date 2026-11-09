using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeCreateRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.MazeCreateReq;

    public int Account { get; }
    public int Actor { get; }
    public int Party { get; }
    public short Location { get; }
    public short Channel { get; }
    public int Jump { get; }
    public int Portal { get; }
    public short World { get; }
    public MapValue Map { get; }
    public ChangeServerType ChangeType { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
