using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.League.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct LeagueValue
{
    public int Id { get; }
    public string Name { get; }

    public PersonLeagueCardValueEntry Card { get; }

    public LeagueValue(LeagueComponent component)
    {
        Id = component.Id;
        Name = component.Name;
        Card = new PersonLeagueCardValueEntry(component.Card);
    }

    public LeagueValue(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Name = reader.ReadUtf8UnicodeString();
        Card = new PersonLeagueCardValueEntry(reader);
    }
}