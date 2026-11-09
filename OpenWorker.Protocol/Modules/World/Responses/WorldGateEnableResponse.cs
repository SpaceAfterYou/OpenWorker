using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct WorldGateEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.GateEnableRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Gate { get; init; }
    public bool Open { get; init; }

    /// <summary>
    /// The maze is won, and the doors that open from now on lead out of it.
    /// </summary>
    public bool IsMazeComplete { get; init; }

    /// <summary>
    /// The sector behind the gate, or zero.
    /// </summary>
    public int NextSector { get; init; }
}
