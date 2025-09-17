using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record CommonPositionBox : BasicEntity
{
    public CommonPositionBox(XElement x) : base(x)
    {
        EntityType = x.GetEnum<EntityType>("m_eEntityType");
        Entity = x.GetInt32("m_iEntityID");
        Group = x.GetInt32("m_iGroup");
    }

    /// <summary>
    /// </summary>
    public EntityType EntityType { get; }

    /// <summary>
    /// </summary>
    public int Entity { get; }

    /// <summary>
    /// </summary>
    public int Group { get; }
}