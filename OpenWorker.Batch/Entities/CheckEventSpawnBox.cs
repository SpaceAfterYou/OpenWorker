using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record CheckEventSpawnBox : BasicEntity
{
    public CheckEventSpawnBox(XElement x) : base(x)
    {
        EventType = x.GetEnum<EventType>("m_eEvent_Type");
        EventRate = x.GetSingle("m_fEvent_Rate");
        EventDelayTime = x.GetSingle("m_fEvent_Delay_Time");
        EventOperation = x.GetInt32("m_iEvent_Operation_ID");
        EventTime = x.GetSingle("m_fEvent_Time");
        SpawnBoxList = Enumerable.Range(0, 5).Select(id => x.GetInt32($"m_iSpawn_Box_ID_{id}")).ToArray();
    }

    /// <summary>
    ///     Object Type
    /// </summary>
    public EventType EventType { get; }

    /// <summary>
    ///     Generated event probability, ten thousand minutes rate(10000=100%)
    /// </summary>
    public float EventRate { get; }

    /// <summary>
    ///     Until the event occurs after the delay time
    /// </summary>
    public float EventDelayTime { get; }

    /// <summary>
    ///     Operation of the event file
    /// </summary>
    public int EventOperation { get; }

    /// <summary>
    ///     The event timeout, ms(1seconds = 1000ms)
    /// </summary>
    public float EventTime { get; }

    /// <summary>
    ///     Spawn Box ID
    /// </summary>
    public IReadOnlyList<int> SpawnBoxList { get; }
}