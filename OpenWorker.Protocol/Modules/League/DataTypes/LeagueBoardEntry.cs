using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// A note on the board of a league.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct LeagueBoardEntry : IWireReadable<LeagueBoardEntry>
{
    public const int MaxMessageLength = 61;

    public LeagueBoardEntry()
    {
        Name = string.Empty;
        Message = string.Empty;
    }

    public int Serial { get; init; }
    public int League { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Message { get; init; }

    /// <summary>
    /// Seconds since 1970.
    /// </summary>
    public long Date { get; init; }

    /// <summary>
    /// Zero, or the number of the text the client shows in place of the answer.
    /// </summary>
    public int Result { get; init; }

    public static LeagueBoardEntry Read(ref PacketReader reader)
    {
        return new LeagueBoardEntry
        {
            Serial = reader.ReadInt32(),
            League = reader.ReadInt32(),
            Name = reader.ReadUtf16UnicodeString(),
            Message = reader.ReadUtf16UnicodeString(),
            Date = reader.ReadInt64(),
            Result = reader.ReadInt32()
        };
    }
}
