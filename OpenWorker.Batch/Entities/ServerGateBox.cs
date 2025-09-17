using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record ServerGateBox : BasicEntity
{
    public ServerGateBox(XElement x) : base(x)
    {
        Type = x.GetEnum<GateType>("m_eType");
        NextSector = x.GetInt32("m_iNextSectorID");
        Sector = x.GetInt32("m_iSectorID");
    }

    /// <summary>
    /// </summary>
    public GateType Type { get; }

    /// <summary>
    /// </summary>
    public int NextSector { get; }

    /// <summary>
    /// </summary>
    public int Sector { get; }
}