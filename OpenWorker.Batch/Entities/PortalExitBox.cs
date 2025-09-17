using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record PortalExitBox : BasicEntity
{
    public PortalExitBox(XElement x) : base(x)
    {
        ParentPortal = x.GetInt32("m_iParentPortalBoxID");
    }

    /// <summary>
    ///     PortalBox ID of connected
    /// </summary>
    public int ParentPortal { get; }
}