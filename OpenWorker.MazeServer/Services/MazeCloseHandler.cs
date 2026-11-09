using Arch.Core;
using OpenWorker.Hotspot;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player who loses the connection or leaves the game leaves the maze: it goes on for the rest of the party, and the
/// last one out ends it.
/// </summary>
public sealed class MazeCloseHandler(MazeInstances instances) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        return instances.LeaveAsync(player);
    }
}
