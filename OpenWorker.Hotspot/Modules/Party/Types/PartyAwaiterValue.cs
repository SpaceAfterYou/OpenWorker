using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Types;

/// <summary>
/// Someone waiting to be picked for a party.
/// </summary>
public readonly struct PartyAwaiterValue(BinaryReader reader) : IWritableData
{
    public ActorValue Actor { get; init; } = new(reader);
    public string Name { get; init; } = reader.ReadPersonName();
    public short Level { get; init; } = reader.ReadInt16();
    public Hero Class { get; init; } = reader.ReadHero();
    public int RemainTime { get; init; } = reader.ReadInt32();

    // The client keeps these two but nothing found so far uses them.
    public int Unknown1 { get; init; } = reader.ReadInt32();
    public int Unknown2 { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.WritePersonName(Name);
        writer.Write(Level);
        writer.Write(Class);
        writer.Write(RemainTime);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
    }
}
