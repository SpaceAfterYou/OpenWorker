using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldGateEnableResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.GateEnableRes;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Event { get; init; }
    public bool IsOpen { get; init; }
    public bool IsMazeComplete { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Event);
        writer.Write(IsOpen);
        writer.Write(IsMazeComplete);
    }

#endregion Interface: IWritableData
}
