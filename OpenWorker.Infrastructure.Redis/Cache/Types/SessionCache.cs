using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class SessionCache
{
    [RedisIdField]
    [Indexed]
    public required long Session { get; init; }

    [Indexed]
    public required int Account { get; init; }

    [Indexed]
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Set when the player leaves the gate for the server list, so that the next
    /// connection to the auth server can continue this session without a login.
    /// </summary>
    public bool ReturnToAuth { get; init; }

    /// <summary>
    /// The address the player left from when <see cref="ReturnToAuth"/> was set. The request that continues the session
    /// carries the number of the account and nothing secret, so only a connection from this address may continue it.
    /// Null when the address of the connection was not known.
    /// </summary>
    public string? ReturnAddress { get; init; }

    /// <summary>
    /// A server holds the connection of this session right now.
    /// </summary>
    public bool Connected { get; init; }

    /// <summary>
    /// One while a connection holds the session, zero after it has closed; what <see cref="Connected"/> says, in the form
    /// a script reads.
    /// </summary>
    public int Held { get; init; }

    /// <summary>
    /// One while the holder is a server the person plays on (a district or a maze). Such a holder has the person in its
    /// world, so the next server waits until it has let the person go; a login or a gate server is simply followed.
    /// </summary>
    public int Play { get; init; }

    /// <summary>
    /// The connection that may renew or release this session.
    /// </summary>
    public string? Owner { get; init; }
}