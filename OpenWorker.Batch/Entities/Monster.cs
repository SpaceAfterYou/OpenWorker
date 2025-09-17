using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public readonly struct Monster(int id, MonsterSpawnType type, int chance)
{
    /// <summary>
    ///     Monster ID
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    ///     Monster Spawn Type
    /// </summary>
    public MonsterSpawnType Type { get; } = type;

    /// <summary>
    ///     Monster Chance(0~10000)
    ///     M11_BREAKOUT_EP_03.vbatch
    ///     M07_CONCRETEJUNGLE_EP_04.vbatch
    ///     <m_iChance4 type="0" value="100000" />
    /// </summary>
    public int Chance { get; } = chance;
}