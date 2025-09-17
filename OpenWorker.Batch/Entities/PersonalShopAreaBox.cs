using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch.Entities;

public sealed record PersonalShopAreaBox : BasicEntity
{
    public PersonalShopAreaBox(XElement x) : base(x)
    {
    }
}