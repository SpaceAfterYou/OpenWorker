namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Work a feature does for its persons from time to time, such as ending what has run out.
/// It runs on the world loop.
/// </summary>
public interface IMiscTick
{
    ValueTask TickAsync(CancellationToken cancellationToken);
}
