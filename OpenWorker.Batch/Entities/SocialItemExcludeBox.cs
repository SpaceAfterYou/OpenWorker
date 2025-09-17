using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch.Entities;

public sealed record SocialItemExcludeBox : BasicEntity
{
    public SocialItemExcludeBox(XElement x) : base(x)
    {
    }
}