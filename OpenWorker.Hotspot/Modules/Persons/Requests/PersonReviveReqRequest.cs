using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PersonReviveReqRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ReviveReq;

    public ActorValue Actor { get; } = new(reader);
    public ReviveType Type { get; } = reader.ReadReviveType();
    
    public MessageOpcode Opcode => new(Group, Command);
}