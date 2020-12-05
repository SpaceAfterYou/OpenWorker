using System;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch
{
    [Flags]
    public enum LuaFunctionType
    {
        Self,
        Party,
        Monster,
        Npc,
        Warp
    }
}
