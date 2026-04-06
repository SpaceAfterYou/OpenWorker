using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct LeagueValue
{
    public int Id { get; }
    public string Name { get; }

    public PersonLeagueCardValueEntry Card { get; }

    public LeagueValue(int id, string name, PersonLeagueCardValueEntry card)
    {
        Id = id;
        Name = name;
        Card = card;
    }

    public LeagueValue(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Name = reader.ReadUtf8UnicodeString();
        Card = new PersonLeagueCardValueEntry(reader);
    }
}
