using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// What the pace check makes of the moves of the players: the ones that cover more ground than the player could have run, which
/// are let through and counted, and the ones that are refused.
/// </summary>
public sealed class MovementMetrics
{
    /// <summary>
    /// A refusal because the move takes the player too far from its place in one message.
    /// </summary>
    public const string Far = "far";

    /// <summary>
    /// A drop of a move from the place the client left, before it has taken the place the server told it.
    /// </summary>
    public const string Stale = "stale";

    private readonly Counter<long> _tooFast;
    private readonly Counter<long> _refused;

    public MovementMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _tooFast = meter.CreateCounter<long>(
            "movement.too_fast",
            unit: "{move}",
            description: "Moves that covered more ground than the player could have run in the time it had. They are let through, as the original server does.");

        _refused = meter.CreateCounter<long>(
            "movement.refused",
            unit: "{move}",
            description: "Moves that were not taken. Tag reason: far (too far in one message), stale (from the place the client left).");
    }

    public void TooFast()
    {
        _tooFast.Add(1);
    }

    /// <param name="reason"><see cref="Far"/> or <see cref="Stale"/>.</param>
    public void Refused(string reason)
    {
        _refused.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }
}
