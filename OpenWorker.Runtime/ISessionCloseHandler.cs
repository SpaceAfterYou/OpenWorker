using Arch.Core;

namespace OpenWorker.Hotspot;

/// <summary>
/// Runs when a client connection ends, before the player entity is destroyed.
/// </summary>
public interface ISessionCloseHandler
{
    ValueTask OnSessionClosedAsync(Entity player);
}
