using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.DistrictServer.Server.Components;

/// <summary>
/// What the server knows about whether a connected player is still there.<br/>
/// Two clocks meet here and never mix: <see cref="AnsweredAt"/> is the clock of this server,
/// <see cref="ClientTickCount"/> is the clock of the client and only travels back to it.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct KeepAliveComponent
{
    /// <summary>
    /// When the client last answered, as a <see cref="TimeProvider.GetTimestamp"/> value of this server.
    /// </summary>
    public long AnsweredAt { get; init; }

    /// <summary>
    /// The tick count the client wrote in its last answer. Nothing is measured with it.
    /// </summary>
    public long ClientTickCount { get; init; }

    /// <summary>
    /// How many keep-alives were sent since the client last answered.
    /// </summary>
    public byte PenaltyCount { get; init; }

    /// <summary>
    /// The state of a player that has just joined: the join counts as the first answer.
    /// </summary>
    public static KeepAliveComponent Start(long now) => new() { AnsweredAt = now };

    /// <summary>
    /// The client answered, so every keep-alive sent so far is forgiven.
    /// </summary>
    public KeepAliveComponent Answer(long now, long clientTickCount) => this with
    {
        AnsweredAt = now,
        ClientTickCount = clientTickCount,
        PenaltyCount = 0
    };
}
