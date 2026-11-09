using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PersonOtherInfoRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.OtherInfo;

    public int Person { get; }
    public bool CheckOption { get; }
    public bool CalculateStat { get; }
    
    public MessageOpcode Opcode => new(Group, (byte)Command);
}