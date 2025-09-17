using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Messages.Response.World;

[HotspotMessage(Group, Command)]
public readonly struct WorldCurrentDateClientMessage : IResponseHotspotMessage
{
    public const GroupOpcode Group = GroupOpcode.World;
    public const WorldOpcode Command = WorldOpcode.CurDate;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(DateTime.UtcNow);
    }
}