using SoulCore.Game.Datas.World.Table.Types;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VMonster
    {
        /// <summary>
        /// Monster ID
        /// </summary>
        public uint Id { get; }

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
        public uint Chance { get; }

        internal VMonster(uint id, MonsterSpawnType type, uint chance) =>
            (Id, Type, Chance) = (id, type, chance);
    }
}
