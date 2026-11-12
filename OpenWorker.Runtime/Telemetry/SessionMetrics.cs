using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// The connections of the process: how many are open, why they end, and what goes through them.<br/>
/// A connection is open from the moment its <c>ServerSessionComponent</c> is made until its writer has sent what it had
/// and closed the socket, and is counted closed exactly once, under the first reason it was given.
/// </summary>
public sealed class SessionMetrics : IFrameDrops
{
    private readonly UpDownCounter<long> _active;
    private readonly Counter<long> _closed;
    private readonly UpDownCounter<long> _writerQueue;
    private readonly Counter<long> _framesSent;
    private readonly Counter<long> _framesDropped;
    private readonly Counter<long> _bytesSent;
    private readonly Counter<long> _bytesReceived;

    public SessionMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _active = meter.CreateUpDownCounter<long>(
            "sessions.active",
            unit: "{session}",
            description: "Connections that are open.");

        _closed = meter.CreateCounter<long>(
            "sessions.closed",
            unit: "{session}",
            description: "Connections that ended. Tag reason: client, server, backpressure, timeout, malformed, shutdown, no_entry.");

        // A sum that goes up and down costs nothing when no one listens, where a counter read by a gauge would
        // have to be an atomic operation on every frame.
        _writerQueue = meter.CreateUpDownCounter<long>(
            "writer.queue.length",
            unit: "{frame}",
            description: "Frames queued to be sent, over all the connections of the process.");

        _framesSent = meter.CreateCounter<long>(
            "frames.sent",
            unit: "{frame}",
            description: "Frames written to a socket.");

        _framesDropped = meter.CreateCounter<long>(
            "frames.dropped",
            unit: "{frame}",
            description: "Messages dropped because the body does not fit one frame. Tag opcode.");

        _bytesSent = meter.CreateCounter<long>(
            "bytes.sent",
            unit: "By",
            description: "Bytes written to sockets, headers included.");

        _bytesReceived = meter.CreateCounter<long>(
            "bytes.received",
            unit: "By",
            description: "Bytes read from sockets, headers included.");
    }

    /// <summary>
    /// The value of the <c>reason</c> tag.
    /// </summary>
    public static string Tag(SessionCloseReason reason)
    {
        return reason switch
        {
            SessionCloseReason.Client => "client",
            SessionCloseReason.Backpressure => "backpressure",
            SessionCloseReason.Timeout => "timeout",
            SessionCloseReason.Malformed => "malformed",
            SessionCloseReason.Shutdown => "shutdown",
            SessionCloseReason.NoEntry => "no_entry",
            _ => "server"
        };
    }

    internal void Opened()
    {
        _active.Add(1);
    }

    internal void Closed(SessionCloseReason reason)
    {
        _active.Add(-1);
        _closed.Add(1, new KeyValuePair<string, object?>("reason", Tag(reason)));
    }

    /// <summary>
    /// A frame went into the queue of a writer.
    /// </summary>
    internal void Queued()
    {
        _writerQueue.Add(1);
    }

    /// <summary>
    /// A frame left the queue of a writer, to be sent or to be dropped.
    /// </summary>
    internal void Dequeued()
    {
        _writerQueue.Add(-1);
    }

    internal void Sent(int bytes)
    {
        _framesSent.Add(1);
        _bytesSent.Add(bytes);
    }

    /// <param name="opcode">The name of the opcode of the message, the tag.</param>
    void IFrameDrops.Dropped(string opcode)
    {
        _framesDropped.Add(1, new KeyValuePair<string, object?>("opcode", opcode));
    }

    public void Received(int bytes)
    {
        _bytesReceived.Add(bytes);
    }
}
