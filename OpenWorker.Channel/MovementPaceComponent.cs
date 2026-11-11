using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Channel;

/// <summary>
/// How much running a player has saved up, and whether the server has told it a place its client has not taken yet, for
/// <see cref="MovementPace"/>. The default value is a player that has not moved yet. Touched on the world loop only.
/// </summary>
[EntityComponent(EntityComponentService.All)]
public struct MovementPaceComponent
{
    /// <summary>
    /// The player has moved once: <see cref="Credit"/> and <see cref="At"/> hold something.
    /// </summary>
    public bool Started { get; internal set; }

    /// <summary>
    /// Seconds of running at the highest speed allowed that the player may still spend.
    /// </summary>
    public float Credit { get; internal set; }

    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp"/> of the last move, when the credit was worked out.
    /// </summary>
    public long At { get; internal set; }

    /// <summary>
    /// A refused move has counted against the error budget, at <see cref="ChargedAt"/>.
    /// </summary>
    public bool Charged { get; internal set; }

    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp"/> of the last refused move that counted against the error budget.
    /// </summary>
    public long ChargedAt { get; internal set; }

    /// <summary>
    /// The server told the client a place (the entry of a map, a warp) at <see cref="ToldAt"/>, and no move of the client
    /// has come from near that place yet.
    /// </summary>
    public bool Told { get; internal set; }

    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp"/> of the last place the server told the client.
    /// </summary>
    public long ToldAt { get; internal set; }
}
