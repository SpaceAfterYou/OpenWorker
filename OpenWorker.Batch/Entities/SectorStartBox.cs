using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record SectorStartBox : BasicEntity
{
    public SectorStartBox(XElement x) : base(x)
    {
        Sector = x.GetInt32("m_nSectorID");
    }

    /// <summary>
    /// </summary>
    public int Sector { get; }
}