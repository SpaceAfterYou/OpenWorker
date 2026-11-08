using BenchmarkDotNet.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Benchmarks;

/// <summary>
/// The step of one player told to a channel of 50: the message is encoded and queued to every session, whose writer
/// sends it to a stream that drops it. What is measured is the work of the world loop plus what the writers allocate.
/// </summary>
[MemoryDiagnoser]
public class BroadcastBenchmark
{
    private const int Recipients = 50;

    private readonly ServerSessionComponent[] _sessions = new ServerSessionComponent[Recipients];

    // Boxed once: the handler boxes its response itself, which is not the send path.
    private readonly IResponseHotspotMessage _message = Movement.Response();

    [GlobalSetup]
    public void Setup()
    {
        for (var index = 0; index < _sessions.Length; index++)
        {
            _sessions[index] = new ServerSessionComponent(Stream.Null);
        }
    }

    /// <summary>
    /// What <c>ServiceChannel.Broadcast</c> does: one frame, queued to every member.
    /// </summary>
    [Benchmark]
    public void EncodeAndQueueToFifty()
    {
        using var frame = Frame.Encode(_message);

        foreach (var session in _sessions)
        {
            session.Send(frame!);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // A writer that fell behind closes its session, and the numbers would be of nothing.
        var closed = _sessions.Count(e => e.Closed.IsCompleted);

        foreach (var session in _sessions)
        {
            session.Disconnect();
        }

        if (closed > 0)
        {
            throw new InvalidOperationException($"{closed} sessions closed during the run.");
        }
    }
}
