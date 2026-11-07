using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A monster disappears from the sight of the clients or comes back.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MonsterInvisibleResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.InvisibleBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public bool Invisible { get; init; }
    public int Flags { get; init; }
    public int Type { get; init; }
    public int Value { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Invisible);
        writer.Write(Flags);
        writer.Write(Type);
        writer.Write(Value);
    }
}
