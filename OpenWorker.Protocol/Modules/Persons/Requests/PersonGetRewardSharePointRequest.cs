using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

/// <summary>
/// The player takes the reward of the progress of a district (type 0) or of a group of mazes (type 1). The client asks
/// only for a district or a group it was told about (<see cref="Responses.PersonLoadDistrictStateResponse"/>,
/// <see cref="Responses.PersonLoadMazeStateResponse"/>, <see cref="Responses.PersonUpdateSharePointResponse"/>). The answer is
/// always <see cref="Responses.PersonGetRewardSharePointResponse"/>.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PersonGetRewardSharePointRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.GetRewardSharePoint;

    public int Type { get; }
    public int Identifier { get; }
    
    public MessageOpcode Opcode => new(Group, (byte)Command);
}