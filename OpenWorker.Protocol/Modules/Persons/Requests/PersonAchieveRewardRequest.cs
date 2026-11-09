using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

/// <summary>
/// The player takes the reward of an achievement of <c>tb_Achievement</c>, from the window of the achievements. The answer
/// is <see cref="Responses.PersonAchieveRewardResponse"/>, an error when the gold, battle points or ether of the reward would
/// overflow or its items do not fit, and nothing for an achievement that is not reached or already rewarded.
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