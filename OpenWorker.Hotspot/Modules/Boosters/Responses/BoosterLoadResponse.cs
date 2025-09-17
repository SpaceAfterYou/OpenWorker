using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Extensions;
using OpenWorker.Hotspot.Modules.Boosters.Types;

namespace OpenWorker.Hotspot.Modules.Boosters.Responses;

[HotspotMessage(Group, Command)]
public readonly struct BoosterLoadResponse(IReadOnlyList<BoosterLoadEntry> entries, BoosterConsumeArea area) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Booster;
    private const BoosterOpcode Command = BoosterOpcode.ListLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(area);
        writer.Write((short)entries.Count);

        foreach (var entry in entries)
        {
            writer.Write(entry.Id);
            writer.Write(entry.Remaining);
        }
    }
}