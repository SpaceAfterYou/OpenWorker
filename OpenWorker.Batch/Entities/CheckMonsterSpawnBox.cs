using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record CheckMonsterSpawnBox : BasicEntity
{
    public CheckMonsterSpawnBox(XElement x) : base(x)
    {
        Type = x.GetEnum<MonsterType>("m_eType");
        LoopCount = x.GetInt32("m_iLoopCount");
        Entity = x.GetInt32("m_iEntityID");
        CheckBoxList = Enumerable.Range(0, 10).Select(id => x.GetInt32($"m_iCheckBox_{id}")).ToArray();
    }

    /// <summary>
    ///     CheckType
    /// </summary>
    public MonsterType Type { get; }

    /// <summary>
    ///     LoopCount
    /// </summary>
    public int LoopCount { get; }

    /// <summary>
    ///     Target ID to work with this box to collide (as CheckType and find the target in combination)
    /// </summary>
    public int Entity { get; }

    /// <summary>
    /// </summary>
    public IReadOnlyList<int> CheckBoxList { get; }
}