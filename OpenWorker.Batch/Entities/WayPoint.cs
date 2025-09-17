using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch.Entities;

public sealed record WayPoint : BasicPoint
{
    public WayPoint(XElement x) : base(x)
    {
    }
}