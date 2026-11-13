using System.Buffers;
using System.Collections.Concurrent;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot;

/// <summary>
/// One message ready for the wire: the header and the encrypted body, in an array of the array pool. A message for many
/// connections is encoded once into a frame, and the same frame is queued to every one of them.<br/>
/// A frame counts its references. <see cref="Encode"/> gives the first one to the caller, who lets it go with
/// <see cref="Dispose"/> once the frame is queued where it goes; every connection that queues the frame takes one more and
/// lets it go when the frame is written to the socket or dropped. The last one to let go gives the array back to the
/// pool, exactly once, and the frame itself goes back to a pool of frames to carry the next message.
/// </summary>
public sealed class Frame : IDisposable
{
    // Frames kept for reuse. More are in use only while many are queued at once; those are left to the collector.
    private const int MaxPooled = 4096;

    private static readonly ConcurrentQueue<Frame> Pool = new();

    private static int s_pooled;

    private ArrayPool<byte> _arrays = ArrayPool<byte>.Shared;
    private byte[] _buffer = [];
    private int _length;
    private int _references;

    private Frame()
    {
    }

    /// <summary>
    /// Header and body, as they go to the socket.
    /// </summary>
    public ReadOnlyMemory<byte> Memory => _buffer.AsMemory(0, _length);

    public int Length => _length;

    /// <summary>
    /// Encodes the message once, on the encoder of the world loop (or of the calling thread, off the loop). The caller holds
    /// the first reference and disposes the frame after queueing it.
    /// </summary>
    /// <remarks>A message given as its struct is written where it is, without a box; one that is already held as
    /// <see cref="IResponseHotspotMessage"/> is written through the interface.</remarks>
    /// <returns><see langword="null"/> for a message whose body does not fit one frame: it is dropped, with a warning.</returns>
    public static Frame? Encode<T>(in T message) where T : IResponseHotspotMessage
    {
        return FrameEncoder.Current.Encode(in message);
    }

    /// <summary>
    /// Takes over an array of the pool that holds a frame of <paramref name="length"/> bytes, with one reference.
    /// </summary>
    internal static Frame Create(byte[] buffer, int length, ArrayPool<byte> arrays)
    {
        if (Pool.TryDequeue(out var frame))
        {
            Interlocked.Decrement(ref s_pooled);
        }

        else
        {
            frame = new Frame();
        }

        frame._arrays = arrays;
        frame._buffer = buffer;
        frame._length = length;

        Volatile.Write(ref frame._references, 1);

        return frame;
    }

    /// <summary>
    /// One more holder: a connection that queues the frame.
    /// </summary>
    internal void AddReference()
    {
        Interlocked.Increment(ref _references);
    }

    /// <summary>
    /// A holder lets go. The last one returns the array to its pool and the frame to the pool of frames.
    /// </summary>
    internal void Release()
    {
        var left = Interlocked.Decrement(ref _references);

        if (left > 0)
        {
            return;
        }

        if (left < 0)
        {
            throw new InvalidOperationException("A frame was let go more times than it was held.");
        }

        var buffer = _buffer;
        var arrays = _arrays;

        _buffer = [];
        _length = 0;
        _arrays = ArrayPool<byte>.Shared;

        arrays.Return(buffer);

        if (Interlocked.Increment(ref s_pooled) <= MaxPooled)
        {
            Pool.Enqueue(this);
        }

        else
        {
            Interlocked.Decrement(ref s_pooled);
        }
    }

    /// <summary>
    /// Lets go of the reference <see cref="Encode"/> gave. Call it once.
    /// </summary>
    public void Dispose()
    {
        Release();
    }
}
