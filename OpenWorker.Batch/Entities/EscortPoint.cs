using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record EscortPoint : BasicPoint
{
    public EscortPoint(XElement x) : base(x)
    {
        Function = x.GetString("m_szFunction");
        EnableEffectPath = x.GetString("m_sEnableEffectPath");
        DisableEffectPath = x.GetString("m_sDisableEffectPath");
    }

    /// <summary>
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// </summary>
    public string EnableEffectPath { get; }

    /// <summary>
    /// </summary>
    public string DisableEffectPath { get; }
}