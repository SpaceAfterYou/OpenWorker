using System;
using System.Collections.Generic;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VSectorBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public SectorType Type { get; }

        /// <summary>
        ///
        /// </summary>
        public string EnterLua { get; }

        /// <summary>
        ///
        /// </summary>
        public ClearType ClearType { get; }

        /// <summary>
        ///
        /// </summary>
        public byte ClearKillRatio { get; }

        /// <summary>
        ///
        /// </summary>
        public string ClearLua { get; }

        /// <summary>
        /// Title to be displayed on the screen when entering the sector
        /// </summary>
        public int SectorTitle { get; }

        /// <summary>
        /// Type of exit sector
        /// </summary>
        public SectorExitType ExitType { get; }

        /// <summary>
        /// ID of exit sector
        /// </summary>
        public int Exit { get; }

        /// <summary>
        /// ID of ralative sector(for monster spwan)
        /// </summary>
        public int RelativeSector { get; }

        /// <summary>
        /// Sectors within the monster kill ratio(1Step)
        /// </summary>
        public IReadOnlyList<byte> ConditionKillRatio { get; }

        internal VSectorBox(XmlNode xml) : base(xml)
        {
            Type = (SectorType)Enum.Parse(typeof(SectorType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            EnterLua = xml.SelectSingleNode("m_szEnterLua").Attributes.GetNamedItem("value").Value;
            ClearType = (ClearType)Enum.Parse(typeof(ClearType), xml.SelectSingleNode("m_eClearType").Attributes.GetNamedItem("value").Value, true);
            ClearKillRatio = byte.Parse(xml.SelectSingleNode("m_iClearKillRatio").Attributes.GetNamedItem("value").Value);
            ClearLua = xml.SelectSingleNode("m_szClearLua").Attributes.GetNamedItem("value").Value;
            SectorTitle = int.Parse(xml.SelectSingleNode("m_iSectorTitle").Attributes.GetNamedItem("value").Value);
            ExitType = (SectorExitType)Enum.Parse(typeof(SectorExitType), xml.SelectSingleNode("m_iSectorExitType").Attributes.GetNamedItem("value").Value, true);
            Exit = int.Parse(xml.SelectSingleNode("m_iSectorExitID").Attributes.GetNamedItem("value").Value);
            RelativeSector = int.Parse(xml.SelectSingleNode("m_iRelativeSectorID").Attributes.GetNamedItem("value").Value);
            ConditionKillRatio = new List<byte>(5)
            {
                byte.Parse(xml.SelectSingleNode("m_iConditionKillRatio_1Step").Attributes.GetNamedItem("value").Value),
                byte.Parse(xml.SelectSingleNode("m_iConditionKillRatio_2Step").Attributes.GetNamedItem("value").Value),
                byte.Parse(xml.SelectSingleNode("m_iConditionKillRatio_3Step").Attributes.GetNamedItem("value").Value),
                byte.Parse(xml.SelectSingleNode("m_iConditionKillRatio_4Step").Attributes.GetNamedItem("value").Value),
                byte.Parse(xml.SelectSingleNode("m_iConditionKillRatio_5Step").Attributes.GetNamedItem("value").Value)
            };
        }
    }
}
