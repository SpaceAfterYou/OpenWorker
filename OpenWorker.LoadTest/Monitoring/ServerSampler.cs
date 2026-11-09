namespace OpenWorker.LoadTest.Monitoring;

/// <summary>
/// One reading of a server process or container. <see cref="CpuPercent" /> is of one core, as <c>docker stats</c> and
/// <c>top</c> give it, so a process busy on two cores reads 200.
/// </summary>
internal sealed record ServerSample(string Name, double Seconds, double CpuPercent, long MemoryBytes);

/// <summary>
/// Reads the CPU and the memory of the servers while the bots run. The readings are optional: a sampler that
/// cannot work says why in its notes, and the run goes on without it.
/// </summary>
internal abstract class ServerSampler
{
    private readonly List<ServerSample> _samples = [];
    private readonly List<string> _notes = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Where the figures come from, for the report.
    /// </summary>
    public abstract string Source { get; }

    public IReadOnlyList<ServerSample> Samples
    {
        get
        {
            lock (_lock)
            {
                return [.. _samples];
            }
        }
    }

    public IReadOnlyList<string> Notes
    {
        get
        {
            lock (_lock)
            {
                return [.. _notes];
            }
        }
    }

    public abstract Task RunAsync(TimeSpan interval, long startTimestamp, CancellationToken cancellation);

    protected void Add(ServerSample sample)
    {
        lock (_lock)
        {
            _samples.Add(sample);
        }
    }

    protected void Note(string note)
    {
        lock (_lock)
        {
            if (!_notes.Contains(note))
            {
                _notes.Add(note);
            }
        }
    }
}
