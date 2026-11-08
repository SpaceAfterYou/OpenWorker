using System.Buffers;
using System.Net.Sockets;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot;

/// <param name="client">The connection of the session.</param>
/// <param name="metrics">Counts the connection and what goes through it; <see langword="null"/> for a session nobody measures.</param>
[EntityComponent(EntityComponentService.All)]
public readonly struct ServerSessionComponent(TcpClient client, SessionMetrics? metrics = null)
{
    private readonly SessionWriter _writer = new(client, metrics);

    /// <summary>
    /// Where the client connected from, for the logs. Empty when the socket was not connected or is gone.
    /// </summary>
    public string? RemoteEndPoint { get; } = DescribeRemote(client);

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
    public void Send(IResponseHotspotMessage message)
    {
        TrySend(message);
    }

    public bool TrySend(IResponseHotspotMessage message)
    {
        var pool = ArrayPool<byte>.Shared;

        var buffer = pool.Rent(MessageHeader.HeaderSize + MessageHeader.MaxContentLength);

        try
        {
            /* using */ var stream = new MemoryStream(buffer);

            /* using */ var writer = new BinaryWriter(stream);

            stream.Position = MessageHeader.HeaderSize;

            writer.Write(message.Opcode);

            message.Write(writer);

            var header = new MessageHeader(stream.Position - MessageHeader.HeaderSize);

            MessageEncryption.Exchange(buffer.AsSpan(MessageHeader.HeaderSize, header.ContentLength), header.Version);

            stream.Position = 0;

            header.ToBinary(writer);

            writer.Flush();

            var length = MessageHeader.HeaderSize + header.ContentLength;

            var frame = pool.Rent(length);

            buffer.AsSpan(0, length).CopyTo(frame);

            return _writer.TryEnqueue(frame, length);
        }

        finally
        {
            pool.Return(buffer);
        }
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
