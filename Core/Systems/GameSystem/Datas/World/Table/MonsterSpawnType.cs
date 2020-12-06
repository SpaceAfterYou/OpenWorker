using System;

namespace Core.Systems.GameSystem.Datas.World.Table
{
    [Flags]
    public enum MonsterSpawnType
    {
        Monster,
        Npc,
        DestructionObject,
        TreasureBox,
        Unit
    }
}
