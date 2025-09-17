using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record MazeEscapeBox : BasicEntity
{
    public MazeEscapeBox(XElement x) : base(x)
    {
        Field = x.GetInt32("m_iField");
        EventObject = x.GetInt32("m_iEventObject");
    }

    /// <summary>
    ///     Field ID
    /// </summary>
    public int Field { get; }

    /// <summary>
    ///     ID of Event Object.
    /// </summary>
    public int EventObject { get; }
}