using System.Xml.Linq;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities.Basic;

public abstract record BasicPoint : BasicEntity
{
    protected BasicPoint(XElement x) : base(x)
    {
        Type = x.GetEnum<PointType>("m_eType");
        BattleType = x.GetEnum<BattleType>("m_eBattleType");
        BeforePoint = x.GetInt32("m_iBeforePoint");
        NextPointList = Enumerable.Range(1, 4).Select(id => x.GetInt32($"m_iNextPoint{(id > 1 ? id : "")}")).ToArray();
        IdleAction = x.GetString("m_szIdleAction");
        IdleActionRatio = x.GetInt32("m_uiIdleActionRatio");
        DelayTime = x.GetInt32("m_uiDelayTime");
        RepeatCount = x.GetByte("m_RepeatCount");
    }

    /// <summary>
    /// </summary>
    public PointType Type { get; }

    /// <summary>
    /// </summary>
    public BattleType BattleType { get; }

    /// <summary>
    /// </summary>
    public int BeforePoint { get; }

    /// <summary>
    /// </summary>
    public IReadOnlyList<int> NextPointList { get; }

    /// <summary>
    /// </summary>
    public string IdleAction { get; }

    /// <summary>
    /// </summary>
    public int IdleActionRatio { get; }

    /// <summary>
    /// </summary>
    public int DelayTime { get; }

    /// <summary>
    /// </summary>
    public byte RepeatCount { get; }
}