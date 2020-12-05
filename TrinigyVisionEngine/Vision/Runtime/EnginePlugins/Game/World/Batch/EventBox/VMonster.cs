namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VMonster
    {
        /// <summary>
        /// Monster ID
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Monster Spawn Type
        /// </summary>
        public MonsterSpawnType Type { get; }

        /// <summary>
        /// Monster Chance(0~10000)
        ///
        /// M07_CONCRETEJUNGLE_EP_04.vbatch
        /// <m_iChance4 type="0" value="100000" />
        /// </summary>
        public int Chance { get; }

        internal VMonster(int id, MonsterSpawnType type, int chance) => (Id, Type, Chance) = (id, type, chance);
    }
}
