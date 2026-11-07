using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// A note on the board of a league.
/// </summary>
public readonly struct LeagueBoardEntry
{
    public const int MaxMessageLength = 61;

    public LeagueBoardEntry()
    {
        Name = string.Empty;
        Message = string.Empty;
    }

    public LeagueBoardEntry(BinaryReader reader)
    {
        Serial = reader.ReadInt32();
        League = reader.ReadInt32();
        Name = reader.ReadPersonName();
        Message = reader.ReadUtf8UnicodeString(MaxMessageLength);
        Date = reader.ReadInt64();
        Result = reader.ReadInt32();
    }

    public int Serial { get; init; }
    public int League { get; init; }
    public string Name { get; init; }
    public string Message { get; init; }

    /// <summary>
    /// Seconds since 1970.
    /// </summary>
    public long Date { get; init; }

    /// <summary>
    /// Zero, or the number of the text the client shows in place of the answer.
    /// </summary>
    public int Result { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Serial);
        writer.Write(League);
        writer.WritePersonName(Name);
        writer.WriteUtf16UnicodeString(Message, MaxMessageLength);
        writer.Write(Date);
        writer.Write(Result);
    }
}
