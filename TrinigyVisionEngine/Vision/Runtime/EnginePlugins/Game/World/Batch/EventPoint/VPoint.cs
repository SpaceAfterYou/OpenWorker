using System;
using System.Collections.Generic;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventPoint
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
        public int BeforePoint { get; }

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<int> NextPoint { get; }

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

            BeforePoint = int.Parse(xml.SelectSingleNode("m_iBeforePoint").Attributes.GetNamedItem("value").Value);

            NextPoint = new List<int>(4)
            {
                int.Parse(xml.SelectSingleNode("m_iNextPoint").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iNextPoint2").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iNextPoint3").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iNextPoint4").Attributes.GetNamedItem("value").Value)
            };

            IdleAction = xml.SelectSingleNode("m_szIdleAction").Attributes.GetNamedItem("value").Value;
            IdleActionRatio = uint.Parse(xml.SelectSingleNode("m_uiIdleActionRatio").Attributes.GetNamedItem("value").Value);
            DelayTime = uint.Parse(xml.SelectSingleNode("m_uiDelayTime").Attributes.GetNamedItem("value").Value);
            RepeatCount = byte.Parse(xml.SelectSingleNode("m_RepeatCount").Attributes.GetNamedItem("value").Value);
        }
    }
}
