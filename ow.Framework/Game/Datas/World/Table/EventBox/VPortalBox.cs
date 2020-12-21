using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VPortalBox : VEntity
    {
        /// <summary>
        /// Visible of GUI(True or False)
        /// </summary>
        public bool ShowGui { get; }

        /// <summary>
        /// tb_Maze_Entry_Gui
        /// </summary>
        public uint Gui { get; }

        /// <summary>
        /// moving type
        /// </summary>
        public JumpType JumpType { get; }

        /// <summary>
        /// Jump map ID
        /// </summary>
        public uint JumpMap { get; }

        /// <summary>
        /// m_ID of VStartEventBox be moved
        /// </summary>
        public uint Jump { get; }

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
        public uint UiString { get; }

        /// <summary>
        /// ID of next sector
        /// </summary>
        public uint NextSector { get; }

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
        /// ClearSector Id
        /// </summary>
        public IReadOnlyList<uint> ClearSectors { get; }

        /// <summary>
        /// ClearSector Chance
        /// </summary>
        public IReadOnlyList<float> ClearSectorChances { get; }

        /// <summary>
        /// MaxUserCount
        /// </summary>
        public uint MaxUserCount { get; }

        /// <summary>
        /// MaxTimeCount
        /// </summary>
        public uint MaxTimeCount { get; }

        internal VPortalBox(XmlNode xml) : base(xml)
        {
            ShowGui = xml.GetBool("m_bShowGUI");
            Gui = xml.GetUInt32("m_iGUI");
            JumpType = xml.GetEnum<JumpType>("m_eJumpType");
            JumpMap = xml.GetUInt32("m_iJumpMap");
            Jump = xml.GetUInt32("m_iJump");
            PortalState = xml.GetEnum<PortalState>("m_eEffectType");
            DisableEffect = xml.GetString("m_szDisableEffect");
            EnableEffect = xml.GetString("m_szEnableEffect");
            UiString = xml.GetUInt32("m_iUIString");
            NextSector = xml.GetUInt32("m_iNextSectorID");
            CallScript = xml.GetBool("m_bCallScript");
            OpenEpisode = xml.GetUInt32("m_uiOpenEpisode");
            CompleteEpisode = xml.GetUInt32("m_uiCompleteEpisode");
            StringOffset = new(xml.GetSingle("m_fStringOffsetX"), xml.GetSingle("m_fStringOffsetY"), xml.GetSingle("m_fStringOffsetZ"));
            ClearSectors = Enumerable.Range(1, 5).Select(id => xml.GetUInt32($"m_iClearSectorID{id}")).ToArray();
            ClearSectorChances = Enumerable.Range(1, 5).Select(id => xml.GetSingle($"m_fClearSectorChance{id}")).ToArray();
            MaxUserCount = xml.GetUInt32("m_iMaxUserCount");
            MaxTimeCount = xml.GetUInt32("m_iMaxTimeCount");
        }
    }
}
