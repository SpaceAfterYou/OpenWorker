using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Extensions;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command)]
public readonly struct WorldWarpMazeResponse(int boxIndex, WarpValue warp, int portalIndex) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpMazeRes;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(boxIndex);
        writer.Write(warp);
        writer.Write(portalIndex);
    }
}