using System.Collections;
using System.Globalization;
using System.Text;

namespace OpenWorker.Hotspot;

/// <summary>
/// Who a log line is about: the session, where it connected from, and the account and the person once the server knows
/// them. One per session, kept in <see cref="ServerSessionComponent"/> and handed to <c>ILogger.BeginScope</c> for the
/// handlers and the close of that session, so that no packet builds a state of its own.<br/>
/// The values are set on the world loop and read by whatever thread writes a line under the scope (a continuation off
/// the loop, a database command): each is one reference, so a reader sees a value before or after a change, never half
/// of one.
/// </summary>
public sealed class SessionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    public const string SessionKey = "Session";
    public const string RemoteKey = "Remote";
    public const string AccountKey = "Account";
    public const string PersonKey = "Person";

    // Boxed once when a value is set, so that a formatter that enumerates the scope does not box per line.
    private object? _session;
    private object? _account;
    private object? _person;

    // The text is built on the first line that prints the scope, and again only after a value changes: a line written off
    // the loop while a value changes builds it from the old values under the old version, and the next line builds it anew.
    private int _version;
    private Text? _text;

    private sealed record Text(int Version, string Value);

    /// <param name="remote">Where the client connected from; <see langword="null"/> when that is not known.</param>
    public SessionLogScope(string? remote)
    {
        Remote = remote;
    }

    public string? Remote { get; }

    /// <summary>
    /// The entity of the session, set once it exists.
    /// </summary>
    public int? Session
    {
        get => (int?)_session;
        set => Change(ref _session, value);
    }

    /// <summary>
    /// The account of the session, set once the client has logged in or been taken over from another server.
    /// </summary>
    public int? Account
    {
        get => (int?)_account;
        set => Change(ref _account, value);
    }

    /// <summary>
    /// The person (the actor of the character) the session plays, set once the person is loaded.
    /// </summary>
    public int? Person
    {
        get => (int?)_person;
        set => Change(ref _person, value);
    }

    public int Count => 4;

    public KeyValuePair<string, object?> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object?>(SessionKey, _session),
        1 => new KeyValuePair<string, object?>(RemoteKey, Remote),
        2 => new KeyValuePair<string, object?>(AccountKey, _account),
        3 => new KeyValuePair<string, object?>(PersonKey, _person),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The scope has four values.")
    };

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
        {
            yield return this[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// What the console prints after <c>=&gt;</c>: the values that are known, as <c>Key=value</c>.
    /// </summary>
    public override string ToString()
    {
        var version = Volatile.Read(ref _version);

        if (_text is { } text && text.Version == version)
        {
            return text.Value;
        }

        var value = Describe();

        _text = new Text(version, value);

        return value;
    }

    private string Describe()
    {
        var text = new StringBuilder();

        Append(text, SessionKey, _session);
        Append(text, RemoteKey, Remote);
        Append(text, AccountKey, _account);
        Append(text, PersonKey, _person);

        return text.ToString();
    }

    private static void Append(StringBuilder text, string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (text.Length > 0)
        {
            text.Append(' ');
        }

        text.Append(key).Append('=').Append(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    private void Change(ref object? field, int? value)
    {
        if ((int?)field == value)
        {
            return;
        }

        field = value;

        Interlocked.Increment(ref _version);
    }
}
