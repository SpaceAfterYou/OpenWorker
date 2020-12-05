using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VPortalBox : VEntity
    {
        /// <summary>
        /// Visible of GUI(True or False)
        /// </summary>
        public bool ShowGui { get; }

        /// <summary>
        /// tb_Maze_Entry_Gui
        /// </summary>
        public int Gui { get; }

        /// <summary>
        /// moving type
        /// </summary>
        public JumpType JumpType { get; }

        /// <summary>
        /// Jump map ID
        /// </summary>
        public int JumpMap { get; }

        /// <summary>
        /// m_ID of VStartEventBox be moved
        /// </summary>
        public int Jump { get; }

        /// <summary>
        /// An initial activation state of the portal(Enable of Disable)
        /// </summary>
        public PortalState PortalState { get; }

        /// <summary>
        /// Output of the non-active state VFX resource path
        /// </summary>
        public string DisableEffect { get; }

        /// <summary>
        /// Output of the active state VFX resource path
        /// </summary>
        public string EnableEffect { get; }

        /// <summary>
        /// UI String
        /// </summary>
        public int UiString { get; }

        /// <summary>
        /// ID of next sector
        /// </summary>
        public int NextSector { get; }

        /// <summary>
        /// Will you call the script?
        /// </summary>
        public bool CallScript { get; }

        /// <summary>
        /// Hold or break the episode activation
        /// </summary>
        public uint OpenEpisode { get; }

        /// <summary>
        /// Deactivated upon completion of the episode
        /// </summary>
        public uint CompleteEpisode { get; }

        /// <summary>
        /// UI String Offset
        /// </summary>
        public Vector3 StringOffset { get; }

        /// <summary>
        /// ClearSector
        /// </summary>
        public IReadOnlyList<VClearSector> ClearSector { get; }

        /// <summary>
        /// MaxUserCount
        /// </summary>
        public int MaxUserCount { get; }

        /// <summary>
        /// MaxTimeCount
        /// </summary>
        public int MaxTimeCount { get; }

        internal VPortalBox(XmlNode xml) : base(xml)
        {
            ShowGui = bool.Parse(xml.SelectSingleNode("m_bShowGUI").Attributes.GetNamedItem("value").Value);
            Gui = int.Parse(xml.SelectSingleNode("m_iGUI").Attributes.GetNamedItem("value").Value);
            JumpType = (JumpType)Enum.Parse(typeof(JumpType), xml.SelectSingleNode("m_eJumpType").Attributes.GetNamedItem("value").Value, true);
            JumpMap = int.Parse(xml.SelectSingleNode("m_iJumpMap").Attributes.GetNamedItem("value").Value);
            Jump = int.Parse(xml.SelectSingleNode("m_iJump").Attributes.GetNamedItem("value").Value);
            PortalState = (PortalState)Enum.Parse(typeof(PortalState), xml.SelectSingleNode("m_eEffectType").Attributes.GetNamedItem("value").Value, true);
            DisableEffect = xml.SelectSingleNode("m_szDisableEffect").Attributes.GetNamedItem("value").Value;
            EnableEffect = xml.SelectSingleNode("m_szEnableEffect").Attributes.GetNamedItem("value").Value;
            UiString = int.Parse(xml.SelectSingleNode("m_iUIString").Attributes.GetNamedItem("value").Value);
            NextSector = int.Parse(xml.SelectSingleNode("m_iNextSectorID").Attributes.GetNamedItem("value").Value);
            CallScript = bool.Parse(xml.SelectSingleNode("m_bCallScript").Attributes.GetNamedItem("value").Value);
            OpenEpisode = uint.Parse(xml.SelectSingleNode("m_uiOpenEpisode").Attributes.GetNamedItem("value").Value);
            CompleteEpisode = uint.Parse(xml.SelectSingleNode("m_uiCompleteEpisode").Attributes.GetNamedItem("value").Value);
            StringOffset = new Vector3(
                float.Parse(xml.SelectSingleNode("m_fStringOffsetX").Attributes.GetNamedItem("value").Value),
                float.Parse(xml.SelectSingleNode("m_fStringOffsetY").Attributes.GetNamedItem("value").Value),
                float.Parse(xml.SelectSingleNode("m_fStringOffsetZ").Attributes.GetNamedItem("value").Value));
            ClearSector = new List<VClearSector>(5)
            {
                new VClearSector(
                    int.Parse(xml.SelectSingleNode("m_iClearSectorID1").Attributes.GetNamedItem("value").Value),
                    float.Parse(xml.SelectSingleNode("m_fClearSectorChance1").Attributes.GetNamedItem("value").Value)),
                new VClearSector(
                    int.Parse(xml.SelectSingleNode("m_iClearSectorID2").Attributes.GetNamedItem("value").Value),
                    float.Parse(xml.SelectSingleNode("m_fClearSectorChance2").Attributes.GetNamedItem("value").Value)),
                new VClearSector(
                    int.Parse(xml.SelectSingleNode("m_iClearSectorID3").Attributes.GetNamedItem("value").Value),
                    float.Parse(xml.SelectSingleNode("m_fClearSectorChance3").Attributes.GetNamedItem("value").Value)),
                new VClearSector(
                    int.Parse(xml.SelectSingleNode("m_iClearSectorID4").Attributes.GetNamedItem("value").Value),
                    float.Parse(xml.SelectSingleNode("m_fClearSectorChance4").Attributes.GetNamedItem("value").Value)),
                new VClearSector(
                    int.Parse(xml.SelectSingleNode("m_iClearSectorID5").Attributes.GetNamedItem("value").Value),
                    float.Parse(xml.SelectSingleNode("m_fClearSectorChance5").Attributes.GetNamedItem("value").Value)),
            };
            MaxUserCount = int.Parse(xml.SelectSingleNode("m_iMaxUserCount").Attributes.GetNamedItem("value").Value);
            MaxTimeCount = int.Parse(xml.SelectSingleNode("m_iMaxTimeCount").Attributes.GetNamedItem("value").Value);
        }
    }
}
