using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Batch.Entities;

public sealed record QuestMoveCheckBox : BasicEntity
{
    public QuestMoveCheckBox(XElement x) : base(x)
    {
        EpisodeCondition = x.GetInt32("m_uiEpisodeCondition");
    }

    /// <summary>
    /// </summary>
    public int EpisodeCondition { get; }
}