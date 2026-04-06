using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeCreateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.MazeCreateReq;

    public int Account { get; init; } = reader.ReadInt32();
    public int Actor { get; init; } = reader.ReadInt32();
    public int Party { get; init; } = reader.ReadInt32();
    public short Location { get; init; } = reader.ReadInt16();
    public short Channel { get; init; } = reader.ReadInt16();
    public int Jump { get; init; } = reader.ReadInt32();
    public int Portal { get; init; } = reader.ReadInt32();
    public short World { get; init; } = reader.ReadInt16();
    public MapValue Map { get; init; } = reader.ReadMapValue();
    public ChangeServerType ChangeType { get; init; } = reader.ReadChangeServerType(); 
    
    public MessageOpcode Opcode => new(Group, Command);
}