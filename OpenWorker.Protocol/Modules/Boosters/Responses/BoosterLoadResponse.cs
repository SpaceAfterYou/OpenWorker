using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Types;

namespace OpenWorker.Hotspot.Modules.Boosters.Responses;

/// <summary>
/// All the boosters of the person. The client forgets the ones it knew, so this also clears them.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct BoosterLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Booster;
    private const BoosterOpcode Command = BoosterOpcode.ListLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public BoosterConsumeArea Area { get; init; }

    public IReadOnlyList<BoosterLoadEntry> Entries { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Area);
        writer.Write(checked((short)Entries.Count));

        foreach (var entry in Entries)
        {
            entry.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
