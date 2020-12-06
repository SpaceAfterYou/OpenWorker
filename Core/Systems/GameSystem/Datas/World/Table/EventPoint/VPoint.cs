using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventPoint
{
    public abstract class VPoint : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public PointType Type { get; }

        /// <summary>
        ///
        /// </summary>
        public BattleType BattleType { get; }

        /// <summary>
        ///
        /// </summary>
        public uint BeforePoint { get; }

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<uint> NextPoint { get; }

        /// <summary>
        ///
        /// </summary>
        public string IdleAction { get; }

        /// <summary>
        ///
        /// </summary>
        public uint IdleActionRatio { get; }

        /// <summary>
        ///
        /// </summary>
        public uint DelayTime { get; }

        /// <summary>
        ///
        /// </summary>
        public byte RepeatCount { get; }

        protected VPoint(XmlNode xml) : base(xml)
        {
            Type = (PointType)Enum.Parse(typeof(PointType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            BattleType = (BattleType)Enum.Parse(typeof(BattleType), xml.SelectSingleNode("m_eBattleType").Attributes.GetNamedItem("value").Value, true);
            BeforePoint = uint.Parse(xml.SelectSingleNode("m_iBeforePoint").Attributes.GetNamedItem("value").Value);
            NextPoint = Enumerable.Range(1, 4).Select(id => uint.Parse(xml.SelectSingleNode($"m_iNextPoint{(id > 1 ? id : "")}").Attributes.GetNamedItem("value").Value)).ToArray();
            IdleAction = xml.SelectSingleNode("m_szIdleAction").Attributes.GetNamedItem("value").Value;
            IdleActionRatio = uint.Parse(xml.SelectSingleNode("m_uiIdleActionRatio").Attributes.GetNamedItem("value").Value);
            DelayTime = uint.Parse(xml.SelectSingleNode("m_uiDelayTime").Attributes.GetNamedItem("value").Value);
            RepeatCount = byte.Parse(xml.SelectSingleNode("m_RepeatCount").Attributes.GetNamedItem("value").Value);
        }
    }
}