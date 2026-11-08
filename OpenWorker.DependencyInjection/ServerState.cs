using Microsoft.Extensions.Hosting;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Whether the server takes players now: the game listener has started and the server has not begun to stop.
/// The readiness check of the health endpoint reads it, and the shutdown sets it.
/// </summary>
internal sealed class ServerState
{
    private volatile bool _listening;
    private volatile bool _draining;

    /// <summary>
    /// A server is draining from the moment the host starts to stop, before any service has stopped: a signal
    /// to stop (SIGTERM from <c>docker stop</c>) turns readiness off at once.
    /// </summary>
    public ServerState(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(BeginDrain);
    }

    /// <summary>
    /// The game listener runs.
    /// </summary>
    public bool IsListening => _listening;

    /// <summary>
    /// The server is shutting down: it takes no new connection and closes the ones it has.
    /// </summary>
    public bool IsDraining => _draining;

    public void MarkListening()
    {
        _listening = true;
    }

    public void MarkNotListening()
    {
        _listening = false;
    }

    public void BeginDrain()
    {
        _draining = true;
    }
}
