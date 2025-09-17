using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record InterActionBox : BasicEntity
{
    public InterActionBox(XElement x) : base(x)
    {
        Interaction = x.GetInt32("m_iInteractionID");
        ObjectKey = x.GetString("m_sObjectKey");
    }

    /// <summary>
    ///     Interactive table ID
    /// </summary>
    public int Interaction { get; }

    /// <summary>
    ///     Interactive ObjectKey
    /// </summary>
    public string ObjectKey { get; }
}