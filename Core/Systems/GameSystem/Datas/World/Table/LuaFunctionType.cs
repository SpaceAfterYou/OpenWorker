using System;

namespace Core.Systems.GameSystem.Datas.World.Table
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
