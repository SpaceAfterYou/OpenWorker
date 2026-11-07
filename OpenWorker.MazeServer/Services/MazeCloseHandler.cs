using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

public sealed class MazeCloseHandler(World world) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (world.Has<LuaMaze>(player))
        {
            world.Get<LuaMaze>(player).Close();
        }

        return ValueTask.CompletedTask;
    }
}
