namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// What kind of server holds the sessions of its connections.
/// </summary>
public sealed class SessionLeaseOptions
{
    /// <summary>
    /// The server has the persons of its connections in a world (a district, a maze). The next server a person goes to
    /// waits until this one has let them go, so a person is never in two worlds.
    /// </summary>
    public bool Play { get; set; }
}
