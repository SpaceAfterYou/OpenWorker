using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// The packets of one session that failed to parse or to handle, kept for <see cref="HandlerErrorBudget" />.
/// Everything runs on the world loop, so nothing here is locked. The default value is a session with no errors.
/// </summary>
[EntityComponent(EntityComponentService.All)]
public struct HandlerErrorsComponent
{
    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp" /> of the first error of the current window.
    /// </summary>
    public long WindowStart { get; internal set; }

    /// <summary>
    /// The errors since <see cref="WindowStart" />; zero while no window is open.
    /// </summary>
    public int Count { get; internal set; }

    /// <summary>
    /// The budget ran out and the connection is closing; the packets still queued are dropped.
    /// </summary>
    public bool Closed { get; internal set; }
}
