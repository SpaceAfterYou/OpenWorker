using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct PersonUpdateCutsceneRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateCutscene;

    public ActorValue Actor { get; }
    public bool Play { get; }
    [WireString(WireStringFormat.AsciiWithoutTerminator, Length = 256)]
    public string Name { get; }
    
    public MessageOpcode Opcode => new(Group, Command);
}