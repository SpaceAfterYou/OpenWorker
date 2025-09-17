using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record CheckSectorBox : BasicEntity
{
    public CheckSectorBox(XElement x) : base(x)
    {
        CheckSector = x.GetInt32("m_iCheckSector");
        Lua = x.GetString("m_szLua");
        CheckGate = x.GetInt32("m_iCheckGate");
    }

    /// <summary>
    /// </summary>
    public int CheckSector { get; }

    /// <summary>
    /// </summary>
    public string Lua { get; }

    /// <summary>
    /// </summary>
    public int CheckGate { get; }
}