using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record StartEventBox : BasicEntity
{
    public StartEventBox(XElement x) : base(x)
    {
        SpawnType = x.GetEnum<SpawnType>("m_eSpawnType");
    }

    /// <summary>
    /// </summary>
    public SpawnType SpawnType { get; }
}