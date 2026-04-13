using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Types;

public readonly struct PartyMemberInfoValue(BinaryReader reader) : IWritableData
{
    public ActorValue Actor { get; init; } = new(reader);
    public string Name { get; init; } = reader.ReadPersonName();
    public byte Level { get; init; } = reader.ReadByte();
    public Hero Class { get; init; } = reader.ReadHero();
    public int Location { get; init; } = reader.ReadInt32();
    public int Channel { get; init; } = reader.ReadInt32();
    public int MaxHealth { get; init; } = reader.ReadInt32();
    public int Health { get; init; } = reader.ReadInt32();
    public bool IsLoggedIn { get; init; } = reader.ReadBoolean();
    public MapValue Map { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Name);
        writer.Write(Level);
        writer.Write(Class);
        writer.Write(Location);
        writer.Write(Channel);
        writer.Write(MaxHealth);
        writer.Write(Health);
        writer.Write(IsLoggedIn);
        writer.Write(Map);
    }
}
