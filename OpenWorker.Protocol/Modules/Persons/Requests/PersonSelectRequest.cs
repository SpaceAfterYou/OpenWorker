using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PersonSelectRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.SelectReq;

    public ActorValue Actor { get; }
    public int Account { get; }
    public bool IsClearTutorial { get; }
    
    public MessageOpcode Opcode => new(Group, (byte)Command);
}