using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct WorldEnterRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.EnterReq;

    public MessageOpcode Opcode => new(Group, Command);

    public int Account { get; } = reader.ReadInt32();
    public ActorValue Actor { get; } = new(reader);
    public int Party { get; } = reader.ReadInt32();
    public short Location { get; } = reader.ReadInt16();
    public short Channel { get; } = reader.ReadInt16();
    public int Jump { get; } = reader.ReadInt32();
    public int Portal { get; } = reader.ReadInt32();
    public MapValue Map { get; } = new(reader);
    public short DistrictWorld { get; } = reader.ReadInt16();
    public ChangeServerType ChangeType { get; } = reader.ReadChangeServerType();
}
