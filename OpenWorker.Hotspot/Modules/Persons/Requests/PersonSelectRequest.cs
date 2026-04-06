using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PersonSelectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.SelectReq;

    public ActorValue Actor { get; } = new(reader); 
    public int Account { get; } = reader.ReadInt32();
    public bool IsClearTutorial { get; } = reader.ReadBoolean();
    
    public MessageOpcode Opcode => new(Group, Command);
}