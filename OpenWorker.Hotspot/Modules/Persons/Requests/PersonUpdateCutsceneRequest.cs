using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PersonUpdateCutsceneRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateCutscene;

    public ActorValue Actor { get; } = new(reader);
    public bool Play { get; } = reader.ReadBoolean();
    public string Name { get; } = reader.ReadAsciiStringWithoutTerminator(256);
    
    public MessageOpcode Opcode => new(Group, Command);
}