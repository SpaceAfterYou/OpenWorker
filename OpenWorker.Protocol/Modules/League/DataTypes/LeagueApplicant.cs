using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// Somebody who asked to join a league. The client sends its name with a length in bytes and the server writes it with
/// a length in characters, so the reading is written by hand.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct LeagueApplicant : IWireReadable<LeagueApplicant>
{
    public LeagueApplicant()
    {
        Name = string.Empty;
    }

    public int League { get; init; }
    public int Actor { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
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

    public static LeagueApplicant Read(ref PacketReader reader)
    {
        return new LeagueApplicant
        {
            League = reader.ReadInt32(),
            Actor = reader.ReadInt32(),
            Name = reader.ReadUtf8UnicodeString(),
            Level = reader.ReadInt16(),
            Date = reader.ReadInt64(),
            Class = reader.ReadByte(),
            Result = reader.ReadInt32()
        };
    }
}
