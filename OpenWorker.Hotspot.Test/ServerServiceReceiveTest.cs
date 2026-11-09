using System.Buffers.Binary;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// How the read loop of a server finds the frames in what a socket gives: a frame cut anywhere waits for its last byte,
/// several frames in one read are all handled, and a header that cannot be right ends the connection at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerServiceReceiveTest
{
    /// <summary>
    /// What the client puts in the version of its headers.
    /// </summary>
    private const ushort ClientVersion = 0;

    /// <summary>
    /// A frame the way the client sends it, for a request of <see cref="ProbeRequest.Records"/> that carries
    /// <paramref name="rest"/>.
    /// </summary>
    private static byte[] Frame(params byte[] rest)
    {
        byte[] content = [(byte)GroupOpcode.System, (byte)ProbeRequest.Command, ProbeRequest.Records, .. rest];

        var frame = new byte[MessageHeader.HeaderSize + content.Length];

        BinaryPrimitives.WriteUInt16LittleEndian(frame, ClientVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), (ushort)frame.Length);

        frame[4] = 1;

        content.CopyTo(frame, MessageHeader.HeaderSize);

        MessageEncryption.Exchange(frame.AsSpan(MessageHeader.HeaderSize), ClientVersion);

        return frame;
    }

    private static byte[] Rest(int length, byte seed) => [.. Enumerable.Range(0, length).Select(e => (byte)(seed + e))];

    /// <summary>
    /// Writes each piece on its own and waits a moment after it, so the server reads them one at a time.
    /// </summary>
    private static async Task WriteInPiecesAsync(TcpClient client, IEnumerable<Memory<byte>> pieces)
    {
        client.NoDelay = true;

        foreach (var piece in pieces)
        {
            await client.GetStream().WriteAsync(piece);

            await Task.Delay(5);
        }
    }

    private static void AssertRecorded(params byte[][] expected)
    {
        var recorded = ProbeHandler.Recorded.ToArray();

        Assert.HasCount(expected.Length, recorded, "Each frame is handled once.");

        for (var index = 0; index < expected.Length; index++)
        {
            CollectionAssert.AreEqual(expected[index], recorded[index], $"Frame {index} reached its handler changed.");
        }
    }

    [TestMethod]
    public async Task AFrameCutAnywhereIsHandledOnceWhenItsLastByteComes()
    {
        ProbeHandler.Recorded.Clear();

        await using var server = new ServerServiceHost();

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        var byteByByte = Rest(40, 1);
        var headerThenBody = Rest(3000, 7);

        var first = Frame(byteByByte);
        var second = Frame(headerThenBody);

        await WriteInPiecesAsync(client, Enumerable.Range(0, first.Length).Select(e => first.AsMemory(e, 1)));

        await MetricAssertions.UntilAsync(() => ProbeHandler.Recorded.Count == 1, "The frame sent byte by byte was not handled.");

        await WriteInPiecesAsync(client, [second.AsMemory(0, MessageHeader.HeaderSize), second.AsMemory(MessageHeader.HeaderSize)]);

        await MetricAssertions.UntilAsync(() => ProbeHandler.Recorded.Count == 2, "The frame sent as a header and a body was not handled.");

        // Long enough for a second handling of either frame to show.
        await Task.Delay(100);

        AssertRecorded(byteByByte, headerThenBody);
    }

    [TestMethod]
    public async Task FramesThatArriveInOneReadAreAllHandledInOrder()
    {
        ProbeHandler.Recorded.Clear();

        await using var server = new ServerServiceHost();

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        var first = Rest(10, 1);
        var second = Rest(2, 100);

        // The second frame ends with the start of a third, which comes later.
        var third = Rest(20, 200);
        var thirdFrame = Frame(third);

        await client.GetStream().WriteAsync((byte[])[.. Frame(first), .. Frame(second), .. thirdFrame.AsSpan(0, 7)]);

        await MetricAssertions.UntilAsync(() => ProbeHandler.Recorded.Count == 2, "The frames of one read were not all handled.");

        await client.GetStream().WriteAsync(thirdFrame.AsMemory(7));

        await MetricAssertions.UntilAsync(() => ProbeHandler.Recorded.Count == 3, "The frame that a read cut was not handled.");

        AssertRecorded(first, second, third);
    }

    [TestMethod]
    public async Task AHeaderThatCannotBeRightEndsTheConnectionAtOnceAsMalformed()
    {
        await using var server = new ServerServiceHost();
        using var closed = server.Metrics.Listen<long>("sessions.closed");

        await server.StartAsync();

        // A version the key table has no keys for, then a length shorter than the header itself. The first claims the
        // largest frame there is: the server must not wait for it.
        byte[][] headers = [[0xFF, 0xFF, 0xFF, 0xFF, 0x01], [0x00, 0x00, 0x04, 0x00, 0x01]];

        foreach (var header in headers)
        {
            using var client = await server.ConnectAsync();

            await client.GetStream().WriteAsync(header);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var read = await client.GetStream().ReadAsync(new byte[1], timeout.Token);

            Assert.AreEqual(0, read, "The server closes the connection instead of waiting for the frame.");
        }

        await MetricAssertions.UntilAsync(() => closed.Sum() == 2, "The connections were not counted closed.");

        Assert.AreEqual(2, closed.Sum("reason", "malformed"));
        Assert.AreEqual(0, ProbeHandler.Handled);
    }
}
