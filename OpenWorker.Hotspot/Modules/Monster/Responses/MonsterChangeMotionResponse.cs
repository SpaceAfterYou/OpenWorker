using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A monster switches to a motion class: standing, hit, down and so on.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MonsterChangeMotionResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.ChangeMotionBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public short Motion { get; init; }

    /// <summary>
    /// For the motions of a hit this is the kind of hit.
    /// </summary>
    public short SubMotion { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Motion);
        writer.Write(SubMotion);
    }
}
