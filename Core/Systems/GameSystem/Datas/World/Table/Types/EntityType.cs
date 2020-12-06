using System;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
{
    [Flags]
    public enum EntityType
    {
        Pc,
        Npc,
        Monster,
        None
    }
}
