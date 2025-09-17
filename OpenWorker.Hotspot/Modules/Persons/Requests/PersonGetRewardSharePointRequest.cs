using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

/// <summary>
/// TODO: Achievement for district completion?
/// </summary>
/// <param name="reader"></param>
[HotspotMessage(Group, Command)]
public readonly struct PersonGetRewardSharePointRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.GetRewardSharePoint;

    public int Type { get; } = reader.ReadInt32();
    public int Identifier { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}