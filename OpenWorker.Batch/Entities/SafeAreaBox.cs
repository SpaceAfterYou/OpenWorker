using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch.Entities;

public sealed record SafeAreaBox : BasicEntity
{
    public SafeAreaBox(XElement x) : base(x)
    {
    }
}