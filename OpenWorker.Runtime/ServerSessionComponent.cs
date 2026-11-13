using System.Net.Sockets;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot;

[EntityComponent(EntityComponentService.All)]
public readonly struct ServerSessionComponent
{
    private readonly SessionWriter _writer;

    /// <param name="client">The connection of the session.</param>
    /// <param name="metrics">Counts the connection and what goes through it; <see langword="null"/> for a session nobody measures.</param>
    public ServerSessionComponent(TcpClient client, SessionMetrics? metrics = null)
    {
        _writer = new SessionWriter(client, metrics);

        LogScope = new SessionLogScope(DescribeRemote(client));
    }

    /// <summary>
    /// A session whose connection is a stream, for the benchmarks and the tests: what is sent is written to the stream.
    /// </summary>
    internal ServerSessionComponent(Stream connection, SessionMetrics? metrics = null)
    {
        _writer = new SessionWriter(connection, metrics);

        LogScope = new SessionLogScope(null);
    }

    /// <summary>
    /// Where the client connected from, for the logs. Empty when the socket was not connected or is gone.
    /// </summary>
    public string? RemoteEndPoint => LogScope?.Remote;

    /// <summary>
    /// The scope of the log lines about this session, one for its whole life; see <see cref="SessionLogScope"/>.
    /// <see langword="null"/> only on a default component, the one <c>TryGet</c> gives for an entity without a session.
    /// </summary>
    public SessionLogScope? LogScope { get; }

    private static string? DescribeRemote(TcpClient client)
    {
        try
        {
            return client.Client?.RemoteEndPoint?.ToString();
        }

        catch (Exception e) when (e is ObjectDisposedException or SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// Queues the message for this connection. Never waits for the client, and a connection that is
    /// already gone just drops the message.
    /// </summary>
    /// <remarks>To send one message to many connections, encode it once with <see cref="Frame.Encode"/> and send the frame.
    /// A message given as its struct is not boxed.</remarks>
    public void Send<T>(in T message) where T : IResponseHotspotMessage
    {
        TrySend(in message);
    }

    /// <returns><see langword="false"/> when the message is not queued: the connection is gone or too far behind, or the
    /// body of the message does not fit one frame (the connection stays open then).</returns>
    public bool TrySend<T>(in T message) where T : IResponseHotspotMessage
    {
        using var frame = Frame.Encode(in message);

        return frame is not null && _writer.TryEnqueue(frame);
    }

    /// <summary>
    /// Queues a frame that is already encoded; the connection takes a reference of its own, so the caller still disposes
    /// its own after queueing the frame to everyone.
    /// </summary>
    public void Send(Frame frame)
    {
        TrySend(frame);
    }

    /// <inheritdoc cref="Send(Frame)"/>
    public bool TrySend(Frame frame)
    {
        return _writer.TryEnqueue(frame);
    }

    /// <summary>
    /// Closes the connection after everything queued has been sent.
    /// </summary>
    /// <param name="reason">Why, for <c>sessions.closed</c>. The first reason a connection is given is kept, and a close that
    /// has no name of its own is the server's.</param>
    public void Disconnect(SessionCloseReason reason = SessionCloseReason.Server)
    {
        _writer.Complete(reason);
    }

    /// <summary>
    /// Gives the connection the reason it is about to close for, without closing it yet: a close that sends its last
    /// messages first (the notice of a shutdown, what the close handlers send) keeps its reason even when the client
    /// fails to take them. The first reason a connection is given is kept.
    /// </summary>
    public void SetCloseReason(SessionCloseReason reason)
    {
        _writer.SetCloseReason(reason);
    }

    /// <summary>
    /// Completes when the connection has sent what was queued and closed: after <see cref="Disconnect"/>, the moment
    /// the client has everything the server sent it.
    /// </summary>
    public Task Closed => _writer.Completed;

    /// <summary>
    /// Closes the connection at once and drops what is queued.
    /// </summary>
    public void Abort()
    {
        _writer.Abort();
    }
}
