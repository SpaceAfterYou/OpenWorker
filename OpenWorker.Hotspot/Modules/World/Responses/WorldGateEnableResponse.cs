using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldGateEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.GateEnableRes;

    public MessageOpcode Opcode => new(Group, Command);

    public int Gate { get; init; }
    public bool Open { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Gate);
        writer.Write(Open);
    }
}
