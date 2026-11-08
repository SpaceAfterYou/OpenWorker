namespace OpenWorker.Channel.Community;

/// <summary>
/// Who may whisper to a person or look at them, as the client keeps it among the options a player saves: a digit for each
/// option, and the first one tells whether the player has saved them at all.
/// </summary>
public static class PersonOptions
{
    public const int Whisper = 21;
    public const int OtherInfo = 26;

    public const byte AllowAll = 0;
    public const byte OnlyFriends = 1;
    public const byte Nobody = 2;

    /// <summary>
    /// A client that finds a lower digit here has never saved the options and sets its own.
    /// </summary>
    private const int SavedMarker = 5;

    /// <summary>
    /// What the person chose for the option, <see cref="AllowAll"/> while they have not chosen.
    /// </summary>
    public static byte Level(IReadOnlyList<byte>? options, int index)
    {
        if (options is null || options.Count <= index || options[0] - '0' < SavedMarker)
        {
            return AllowAll;
        }

        var level = options[index] - '0';

        return level is OnlyFriends or Nobody ? (byte)level : AllowAll;
    }
}
