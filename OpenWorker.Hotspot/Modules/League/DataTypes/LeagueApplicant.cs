using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// Somebody who asked to join a league.
/// </summary>
public readonly struct LeagueApplicant
{
    public LeagueApplicant()
    {
        Name = string.Empty;
    }

    public LeagueApplicant(BinaryReader reader)
    {
        League = reader.ReadInt32();
        Actor = reader.ReadInt32();
        Name = reader.ReadPersonName();
        Level = reader.ReadInt16();
        Date = reader.ReadInt64();
        Class = reader.ReadByte();
        Result = reader.ReadInt32();
    }

    public int League { get; init; }
    public int Actor { get; init; }
    public string Name { get; init; }
    public short Level { get; init; }

    /// <summary>
    /// Seconds since 1970.
    /// </summary>
    public long Date { get; init; }
    public byte Class { get; init; }

    /// <summary>
    /// Zero, or the number of the text the client shows in place of the answer.
    /// </summary>
    public int Result { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(League);
        writer.Write(Actor);
        writer.WritePersonName(Name);
        writer.Write(Level);
        writer.Write(Date);
        writer.Write(Class);
        writer.Write(Result);
    }
}
