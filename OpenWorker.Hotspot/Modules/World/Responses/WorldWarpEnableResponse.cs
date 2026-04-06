using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Extensions;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldWarpEnableResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpEnableRes;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public WarpValue Warp { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Warp);
    }

#endregion Interface: IWritableData
}
