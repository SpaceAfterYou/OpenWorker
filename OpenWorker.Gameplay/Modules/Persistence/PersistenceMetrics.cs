using System.Diagnostics.Metrics;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// The players who left while the database did not take what they changed: how many wait, and how their wait ended.
/// </summary>
public sealed class PersistenceMetrics
{
    private static KeyValuePair<string, object?> Written { get; } = new("outcome", "written");
    private static KeyValuePair<string, object?> Lost { get; } = new("outcome", "lost");

    private readonly UpDownCounter<long> _kept;
    private readonly Counter<long> _ended;

    public PersistenceMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _kept = meter.CreateUpDownCounter<long>(
            "persistence.kept",
            unit: "{person}",
            description: "Players who left while what they changed waits to be written; their sessions stay held.");

        _ended = meter.CreateCounter<long>(
            "persistence.kept.ended",
            unit: "{person}",
            description: "Players who left whose changes waited, by how it ended. Tag outcome: written, lost.");
    }

    internal void Kept()
    {
        _kept.Add(1);
    }

    /// <summary>
    /// A wait has ended: the changes were written late, or given up.
    /// </summary>
    internal void Ended(bool written)
    {
        _kept.Add(-1);
        _ended.Add(1, written ? Written : Lost);
    }

    /// <summary>
    /// The changes of a player who left were lost without a wait: the server keeps as many as it may already.
    /// </summary>
    internal void Refused()
    {
        _ended.Add(1, Lost);
    }
}
