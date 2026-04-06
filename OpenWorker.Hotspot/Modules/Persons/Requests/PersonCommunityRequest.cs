using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PersonCommunityRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.Community;

    public CommunityState State { get; } = reader.ReadCommunityState();
    public string Comment { get; } = reader.ReadCommunityComment();
    public string Memo { get; } = reader.ReadCommunityMemo();
    
    public MessageOpcode Opcode => new(Group, Command);
}