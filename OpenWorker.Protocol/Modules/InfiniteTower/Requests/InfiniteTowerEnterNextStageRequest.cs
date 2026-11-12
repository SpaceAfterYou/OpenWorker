using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.InfiniteTower.Requests;

/// <summary>
/// The person has cleared a stage of the tower and goes on: into the next stage of the chapter, or out of the tower after
/// the last one. It has no content.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct InfiniteTowerEnterNextStageRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.InfiniteTower;
    private const InfiniteTowerOpcode Command = InfiniteTowerOpcode.EnterNextStage;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
