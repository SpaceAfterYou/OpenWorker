using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldOtherInfosMonsterResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.OtherInfosMonster;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<STMonsterInfo> Monsters { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)Monsters.Count);

        foreach (var item in Monsters)
        {
            writer.WriteMonster(item);
        }
    }
}
