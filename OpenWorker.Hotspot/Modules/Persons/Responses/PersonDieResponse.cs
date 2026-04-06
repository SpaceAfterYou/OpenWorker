using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonDieResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.Die;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorComponent Victim { get; init; }
    public ActorComponent Attacker { get; init; }
    public int PvpKillCount { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Victim);
        writer.WriteActor(Attacker);
        writer.Write(PvpKillCount);
    }

#endregion Interface: IWritableData
}
