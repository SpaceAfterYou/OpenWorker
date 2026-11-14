using System.Diagnostics.CodeAnalysis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What a person allows the others, as the client keeps it among the options a player saves: a digit for each option, and
/// the first one tells whether the player has saved them at all.
/// </summary>
public static class PersonOptions
{
    public const int Whisper = 21;
    public const int FriendRequest = 25;
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
        if (!IsSaved(options, index))
        {
            return AllowAll;
        }

        var level = options[index] - '0';

        return level is OnlyFriends or Nobody ? (byte)level : AllowAll;
    }

    /// <summary>
    /// Whether the person takes requests to be friends. The client keeps a yes (1) or a no (0) here and takes them until the
    /// player chooses; any digit but 1 or 2 refuses.
    /// </summary>
    public static bool TakesFriendRequests(IReadOnlyList<byte>? options)
    {
        return !IsSaved(options, FriendRequest) || options[FriendRequest] - '0' is 1 or 2;
    }

    /// <summary>
    /// What the client of a game master writes over whispers, trades, parties, leagues, friends and looks at every entry
    /// before it sends the options: nobody, and no friend requests. It is not what the player chose.
    /// </summary>
    private static readonly (int Index, byte Forced, byte Default)[] StaffForced =
    [
        (Whisper, (byte)'2', (byte)'0'),
        (22, (byte)'2', (byte)'0'),
        (23, (byte)'2', (byte)'0'),
        (24, (byte)'2', (byte)'0'),
        (FriendRequest, (byte)'0', (byte)'1'),
        (OtherInfo, (byte)'2', (byte)'0')
    ];

    /// <summary>
    /// The options a game master's client sent, with each of those its client forces kept as the player had it (or the
    /// client's default when it never saved them): a game master is not refused for what its client forces, and the account
    /// keeps its choice. A forced option the player changes to something else is taken.
    /// </summary>
    public static byte[] KeepChoiceOfStaff(byte[] sent, IReadOnlyList<byte>? kept)
    {
        var options = sent.ToArray();

        foreach (var (index, forced, value) in StaffForced)
        {
            if (options.Length > index && options[index] == forced)
            {
                options[index] = IsSaved(kept, index) ? kept[index] : value;
            }
        }

        return options;
    }

    private static bool IsSaved([NotNullWhen(true)] IReadOnlyList<byte>? options, int index)
    {
        return options is not null && options.Count > index && options[0] - '0' >= SavedMarker;
    }
}
