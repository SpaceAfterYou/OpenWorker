using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
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
        public uint SectorTitle { get; }

        /// <summary>
        /// Type of exit sector
        /// </summary>
        public SectorExitType ExitType { get; }

        /// <summary>
        /// ID of exit sector
        /// </summary>
        public uint Exit { get; }

        /// <summary>
        /// ID of ralative sector(for monster spwan)
        /// </summary>
        public uint RelativeSector { get; }

        /// <summary>
        /// Sectors within the monster kill ratio step
        /// </summary>
        public IReadOnlyList<byte> ConditionKillRatioStep { get; }

        internal VSectorBox(XmlNode xml) : base(xml)
        {
            Type = (SectorType)Enum.Parse(typeof(SectorType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            EnterLua = xml.SelectSingleNode("m_szEnterLua").Attributes.GetNamedItem("value").Value;
            ClearType = (ClearType)Enum.Parse(typeof(ClearType), xml.SelectSingleNode("m_eClearType").Attributes.GetNamedItem("value").Value, true);
            ClearKillRatio = byte.Parse(xml.SelectSingleNode("m_iClearKillRatio").Attributes.GetNamedItem("value").Value);
            ClearLua = xml.SelectSingleNode("m_szClearLua").Attributes.GetNamedItem("value").Value;
            SectorTitle = uint.Parse(xml.SelectSingleNode("m_iSectorTitle").Attributes.GetNamedItem("value").Value);
            ExitType = (SectorExitType)Enum.Parse(typeof(SectorExitType), xml.SelectSingleNode("m_iSectorExitType").Attributes.GetNamedItem("value").Value, true);
            Exit = uint.Parse(xml.SelectSingleNode("m_iSectorExitID").Attributes.GetNamedItem("value").Value);
            RelativeSector = uint.Parse(xml.SelectSingleNode("m_iRelativeSectorID").Attributes.GetNamedItem("value").Value);
            ConditionKillRatioStep = Enumerable.Range(1, 5).Select(id => byte.Parse(xml.SelectSingleNode($"m_iConditionKillRatio_{id}Step").Attributes.GetNamedItem("value").Value)).ToArray();
        }
    }
}