using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

/// <summary>
/// A skill being used. The client sends it without the actor and with a number the server does not send back; the
/// server sends the actor in its place. Not declarative, so read and written by hand.
/// </summary>
public readonly struct ActiveSkillValue : IWritableData, IWireReadable<ActiveSkillValue>, IWireWritable
{
#region Message: Body

    public ActorValue Actor { get; init; }
    public int Skill { get; init; }
    public int Unknown { get; init; }
    public SkillPositionValue Position { get; init; }
    public int Divergence { get; init; }
    public int ParentSkill { get; init; }
    public byte SkillDeckIndex { get; init; }
    public int RandomTrapIndex { get; init; }

#endregion Message: Body

    public ActiveSkillValue(BinaryReader reader)
    {
        this = WireBridge.Read<ActiveSkillValue>(reader);
    }

    /// <summary>
    /// What the client sends: no actor, and <see cref="Unknown"/> after the skill.
    /// </summary>
    public static ActiveSkillValue Read(ref PacketReader reader)
    {
        return new ActiveSkillValue
        {
            Skill = reader.ReadInt32(),
            Unknown = reader.ReadInt32(),
            Position = SkillPositionValue.Read(ref reader),
            Divergence = reader.ReadInt32(),
            ParentSkill = reader.ReadInt32(),
            SkillDeckIndex = reader.ReadByte(),
            RandomTrapIndex = reader.ReadInt32()
        };
    }

    /// <summary>
    /// What the server sends: the actor first, and no <see cref="Unknown"/>.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Actor);
        writer.WriteInt32(Skill);

        Position.Write(ref writer);

        writer.WriteInt32(Divergence);
        writer.WriteInt32(ParentSkill);
        writer.WriteByte(SkillDeckIndex);
        writer.WriteInt32(RandomTrapIndex);
    }

#region Interface: IWritableData

    public void Write(BinaryWriter writer) => WireBridge.Write(writer, this);

#endregion Interface: IWritableData
}
