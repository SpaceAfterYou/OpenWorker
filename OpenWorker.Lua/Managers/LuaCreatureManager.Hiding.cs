using Arch.Core;
using OpenWorker.Hotspot;

namespace OpenWorker.Lua.Managers;

public sealed partial class LuaCreatureManager
{
    /// <summary>
    /// A player who comes in sees every monster in the list, so the ones that are gone are hidden for that player straight after it.
    /// </summary>
    private void ShowHidingsTo(ServerSessionComponent session, Entity[] monsters)
    {
        foreach (var monster in monsters)
        {
            if (Bodies.TryGetValue(monster, out var body) && body.HidingNotice() is { } notice)
            {
                session.Send(notice);
            }
        }
    }
}
