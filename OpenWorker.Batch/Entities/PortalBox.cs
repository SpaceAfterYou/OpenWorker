using System.Numerics;
using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record PortalBox : BasicEntity
{
    public PortalBox(XElement x) : base(x)
    {
        ShowGui = x.GetBool("m_bShowGUI");
        Gui = x.GetInt32("m_iGUI");
        JumpType = x.GetEnum<JumpType>("m_eJumpType");
        JumpMap = x.GetInt32("m_iJumpMap");
        Jump = x.GetInt32("m_iJump");
        PortalState = x.GetEnum<PortalState>("m_eEffectType");
        DisableEffect = x.GetString("m_szDisableEffect");
        EnableEffect = x.GetString("m_szEnableEffect");
        UiString = x.GetInt32("m_iUIString");
        NextSector = x.GetInt32("m_iNextSectorID");
        CallScript = x.GetBool("m_bCallScript");
        OpenEpisode = x.GetInt32("m_uiOpenEpisode");
        CompleteEpisode = x.GetInt32("m_uiCompleteEpisode");
        StringOffset = new Vector3(x.GetSingle("m_fStringOffsetX"), x.GetSingle("m_fStringOffsetY"),
            x.GetSingle("m_fStringOffsetZ"));
        ClearSectorList = Enumerable.Range(1, 5).Select(id => x.GetInt32($"m_iClearSectorID{id}")).ToArray();
        ClearSectorChanceList = Enumerable.Range(1, 5).Select(id => x.GetSingle($"m_fClearSectorChance{id}")).ToArray();

        try
        {
            MaxUserCount = x.GetInt32("m_iMaxUserCount");
        }
        catch (XmlParsingException)
        {
            MaxUserCount = 0;
        }

        try
        {
            MaxTimeCount = x.GetInt32("m_iMaxTimeCount");
        }
        catch (XmlParsingException)
        {
            MaxTimeCount = 0;
        }
    }

    /// <summary>
    ///     Visible of GUI(True or False)
    /// </summary>
    public bool ShowGui { get; }

    /// <summary>
    ///     tb_Maze_Entry_Gui
    /// </summary>
    public int Gui { get; }

    /// <summary>
    ///     moving type
    /// </summary>
    public JumpType JumpType { get; }

    /// <summary>
    ///     Jump map ID
    /// </summary>
    public int JumpMap { get; }

    /// <summary>
    ///     m_ID of VStartEventBox be moved
    /// </summary>
    public int Jump { get; }

    /// <summary>
    ///     An initial activation state of the portal(Enable of Disable)
    /// </summary>
    public PortalState PortalState { get; }

    /// <summary>
    ///     Output of the non-active state VFX resource path
    /// </summary>
    public string DisableEffect { get; }

    /// <summary>
    ///     Output of the active state VFX resource path
    /// </summary>
    public string EnableEffect { get; }

    /// <summary>
    ///     UI String
    /// </summary>
    public int UiString { get; }

    /// <summary>
    ///     ID of next sector
    /// </summary>
    public int NextSector { get; }

    /// <summary>
    ///     Will you call the script?
    /// </summary>
    public bool CallScript { get; }

    /// <summary>
    ///     Hold or break the episode activation
    /// </summary>
    public int OpenEpisode { get; }

    /// <summary>
    ///     Deactivated upon completion of the episode
    /// </summary>
    public int CompleteEpisode { get; }

    /// <summary>
    ///     UI String Offset
    /// </summary>
    public Vector3 StringOffset { get; }

    /// <summary>
    ///     ClearSector Id
    /// </summary>
    public IReadOnlyList<int> ClearSectorList { get; }

    /// <summary>
    ///     ClearSector Chance
    /// </summary>
    public IReadOnlyList<float> ClearSectorChanceList { get; }

    /// <summary>
    ///     MaxUserCount
    /// </summary>
    public int MaxUserCount { get; }

    /// <summary>
    ///     MaxTimeCount
    /// </summary>
    public int MaxTimeCount { get; }
}