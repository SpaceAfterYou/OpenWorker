using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

/// <summary>
/// TODO: Time before teleport to the next sector?
/// </summary>
[HotspotMessage(Group, Command)]
public readonly struct WorldWarpMazeTimeResponse(TimeSpan time) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpMazeTimeRes;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(time);
    }
}