using System.Buffers.Binary;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A message on an opcode no real message uses, with a number of its own as its body. The frames of a connection reach it
/// through the queue of its writer in the order they were sent, so once a marker has arrived, everything sent to that
/// connection before the marker has arrived too, and what has not was never sent. A test waits for the marker it sent
/// itself, never for any marker: one left by an earlier wait is already there.
/// </summary>
internal sealed class QueueMarker(int number) : IResponseHotspotMessage
{
    public static readonly MessageOpcode Code = new(GroupOpcode.System, 0x7B);

    private static int _last;

    public int Number { get; } = number;

    public MessageOpcode Opcode => Code;

    /// <summary>
    /// A marker with a number no other marker of the test run has.
    /// </summary>
    public static QueueMarker Next() => new(Interlocked.Increment(ref _last));

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Number);
    }

    /// <summary>
    /// Whether the frame is this marker.
    /// </summary>
    public bool Is(PlayerFrame frame) => frame.Opcode.Value == Code.Value && BinaryPrimitives.ReadInt32LittleEndian(frame.Body) == Number;

    /// <summary>
    /// Sends a marker to the connection of the session and waits until it has arrived: everything sent to the session
    /// before has arrived then.
    /// </summary>
    public static async Task SettleAsync(ServerSessionComponent session, RecordingConnection connection)
    {
        var marker = Next();

        session.Send(marker);

        await MetricAssertions.UntilAsync(() => connection.Frames.Any(e => marker.Is(PlayerFrame.Open(e))), "The connection gets the marker.");
    }
}
