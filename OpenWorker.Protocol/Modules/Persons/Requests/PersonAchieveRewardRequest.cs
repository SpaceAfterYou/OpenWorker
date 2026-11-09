using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

/// <summary>
/// The player takes the reward of an achievement of <c>tb_Achievement</c>, from the window of the achievements. The server
/// keeps no progress of achievements and no record of the rewards taken, so it cannot tell a finished one and the request
/// has no handler.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PersonAchieveRewardRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.AchieveReward;
    
    public int Index { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}