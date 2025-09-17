using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record SectorBox : BasicEntity
{
    public SectorBox(XElement x) : base(x)
    {
        Type = x.GetEnum<SectorType>("m_eType");
        EnterLua = x.GetString("m_szEnterLua");
        ClearType = x.GetEnum<ClearType>("m_eClearType");
        ClearKillRatio = x.GetInt16("m_iClearKillRatio");
        ClearLua = x.GetString("m_szClearLua");
        SectorTitle = x.GetInt32("m_iSectorTitle");
        ExitType = x.GetEnum<SectorExitType>("m_iSectorExitType");
        Exit = x.GetInt32("m_iSectorExitID");
        RelativeSector = x.GetInt32("m_iRelativeSectorID");
        ConditionKillRatioSteps = Enumerable
            .Range(1, 5)
            .Select(id => x.GetByte($"m_iConditionKillRatio_{id}Step"))
            .ToArray();
    }

    /// <summary>
    /// </summary>
    public SectorType Type { get; }

    /// <summary>
    /// </summary>
    public string EnterLua { get; }

    /// <summary>
    /// </summary>
    public ClearType ClearType { get; }

    /// <summary>
    /// </summary>
    public short ClearKillRatio { get; }

    /// <summary>
    /// </summary>
    public string ClearLua { get; }

    /// <summary>
    ///     Title to be displayed on the screen when entering the sector
    /// </summary>
    public int SectorTitle { get; }

    /// <summary>
    ///     Type of exit sector
    /// </summary>
    public SectorExitType ExitType { get; }

    /// <summary>
    ///     ID of exit sector
    /// </summary>
    public int Exit { get; }

    /// <summary>
    ///     ID of ralative sector(for monster spwan)
    /// </summary>
    public int RelativeSector { get; }

    /// <summary>
    ///     Sectors within the monster kill ratio step
    /// </summary>
    public IReadOnlyList<byte> ConditionKillRatioSteps { get; }
}