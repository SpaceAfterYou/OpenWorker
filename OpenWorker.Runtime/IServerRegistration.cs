namespace OpenWorker.Hotspot;

/// <summary>
/// What a server tells the others about itself while it runs, or keeps alive on their behalf: the record of a
/// district, of the channels, of a maze server, of a gate, the leases of the sessions. When the server begins to stop,
/// the drain asks each of them to withdraw, before it closes the sessions: the others stop sending players
/// to a server that no longer accepts them.
/// </summary>
public interface IServerRegistration
{
    /// <summary>
    /// Stops renewing the record and removes it. Runs once: a second call returns the first one's outcome.
    /// A record that cannot be removed (Redis is down) expires by itself, so a failure is logged and not thrown.
    /// </summary>
    Task WithdrawAsync(CancellationToken cancellationToken);
}
