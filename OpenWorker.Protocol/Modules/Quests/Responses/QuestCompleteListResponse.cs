using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Quests.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct QuestCompleteListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Quest;
    private const QuestOpcode Command = QuestOpcode.CompleteList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// 128-byte bitset of completed episodes.
    /// </summary>
    public ReadOnlyMemory<byte> CompletedEpisodeBits { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(QuestModuleDefines.CompleteListByteLength == CompletedEpisodeBits.Length);

        writer.Write(CompletedEpisodeBits.Span);
    }

#endregion Interface: IWritableData
}
