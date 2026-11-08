using System.Globalization;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Where a person is right now, as the other servers see it. Kept in Redis while the person is online.
/// </summary>
/// <param name="Whisper">Who may whisper to the person, see <see cref="PersonOptions"/>.</param>
/// <param name="OtherInfo">Who may look at the person's equipment.</param>
public sealed record PresenceRecord(
    Guid Instance,
    int Person,
    string Name,
    byte Hero,
    byte Level,
    short Location,
    short Channel,
    bool InMaze,
    byte Whisper,
    byte OtherInfo)
{
    private const char Separator = '\n';

    /// <summary>
    /// A name never contains a line break, so the fields are told apart by it.
    /// </summary>
    public string Encode()
    {
        return string.Join(
            Separator,
            Instance.ToString("N"),
            Person.ToString(CultureInfo.InvariantCulture),
            Name,
            Hero.ToString(CultureInfo.InvariantCulture),
            Level.ToString(CultureInfo.InvariantCulture),
            Location.ToString(CultureInfo.InvariantCulture),
            Channel.ToString(CultureInfo.InvariantCulture),
            InMaze ? "1" : "0",
            Whisper.ToString(CultureInfo.InvariantCulture),
            OtherInfo.ToString(CultureInfo.InvariantCulture));
    }

    public static PresenceRecord? Decode(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var parts = value.Split(Separator);

        if (parts.Length != 10 ||
            !Guid.TryParseExact(parts[0], "N", out var instance) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var person) ||
            !byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hero) ||
            !byte.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ||
            !short.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var location) ||
            !short.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var channel) ||
            !byte.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var whisper) ||
            !byte.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var info))
        {
            return null;
        }

        return new PresenceRecord(instance, person, parts[2], hero, level, location, channel, parts[7] == "1", whisper, info);
    }

    /// <summary>
    /// Names are compared without regard to case, as the client does when it looks for a player.
    /// </summary>
    public static string NameKey(string name) => name.Trim().ToUpperInvariant();
}
