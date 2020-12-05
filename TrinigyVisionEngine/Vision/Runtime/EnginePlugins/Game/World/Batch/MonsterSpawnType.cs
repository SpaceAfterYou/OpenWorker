using System;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch
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
