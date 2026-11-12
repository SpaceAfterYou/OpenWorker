using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.InfiniteTower.Requests;

/// <summary>
/// The person asks, from the window of the tower in a district, to climb a chapter of the tower: the first of its five stages
/// is entered.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct InfiniteTowerEnterChapterRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.InfiniteTower;
    private const InfiniteTowerOpcode Command = InfiniteTowerOpcode.EnterChapter;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Chapter { get; }
}
