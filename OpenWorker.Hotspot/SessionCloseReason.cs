namespace OpenWorker.Hotspot;

/// <summary>
/// Why a connection ended. The first reason a connection is given is the one it keeps, so a client that is turned away for
/// a full queue and then reports a reset socket is counted as <see cref="Backpressure"/>, once.<br/>
/// The name of each reason is the value of the <c>reason</c> tag of <c>sessions.closed</c>: to add a reason, add it here and
/// give it a name in <c>SessionMetrics.Tag</c>.
/// </summary>
public enum SessionCloseReason : byte
{
    /// <summary>
    /// Not closed yet.
    /// </summary>
    None = 0,

    /// <summary>
    /// The client closed the connection, or the socket failed: end of the stream, a reset, a write that went wrong.
    /// </summary>
    Client,

    /// <summary>
    /// The server closed it for a reason of its own that has no name: a rejected login, a refused reservation, a failure of the read loop.
    /// </summary>
    Server,

    /// <summary>
    /// The client does not read what the server sends: its queue of frames is full, or a write timed out.
    /// </summary>
    Backpressure,

    /// <summary>
    /// The client stopped answering keep-alives.
    /// </summary>
    Timeout,

    /// <summary>
    /// The client sent too many packets that failed to parse or to handle (<c>HandlerErrorBudget</c>).
    /// </summary>
    Malformed
}
