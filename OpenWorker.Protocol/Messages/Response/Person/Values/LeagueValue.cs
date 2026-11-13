using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct LeagueValue : IWireReadable<LeagueValue>
{
    public int Id { get; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }

    public PersonLeagueCardValueEntry Card { get; }

    public LeagueValue(int id, string name, PersonLeagueCardValueEntry card)
    {
        Id = id;
        Name = name;
        Card = card;
    }

    /// <summary>
    /// What the client sends of a new character.
    /// </summary>
    public static LeagueValue Read(ref PacketReader reader)
    {
        var id = reader.ReadInt32();
        var name = reader.ReadUtf16UnicodeString();
        var card = PersonLeagueCardValueEntry.Read(ref reader);

        return new LeagueValue(id, name, card);
    }
}
