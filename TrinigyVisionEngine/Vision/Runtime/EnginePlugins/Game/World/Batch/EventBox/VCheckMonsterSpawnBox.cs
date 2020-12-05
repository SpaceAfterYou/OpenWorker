using System;
using System.Collections.Generic;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VCheckMonsterSpawnBox : VEntity
    {
        /// <summary>
        /// CheckType
        /// </summary>
        public MonsterType Type { get; }

        /// <summary>
        /// LoopCount
        /// </summary>
        public int LoopCount { get; }

        /// <summary>
        /// Target ID to work with this box to collide (as CheckType and find the target in combination)
        /// </summary>
        public int Entity { get; }

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<int> CheckBox { get; }

        internal VCheckMonsterSpawnBox(XmlNode xml) : base(xml)
        {
            Type = (MonsterType)Enum.Parse(typeof(MonsterType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            LoopCount = int.Parse(xml.SelectSingleNode("m_iLoopCount").Attributes.GetNamedItem("value").Value);
            Entity = int.Parse(xml.SelectSingleNode("m_iEntityID").Attributes.GetNamedItem("value").Value);

            CheckBox = new List<int>(10)
            {
                int.Parse(xml.SelectSingleNode("m_iCheckBox_0").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_1").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_2").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_3").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_4").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_5").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_6").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_7").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_8").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iCheckBox_9").Attributes.GetNamedItem("value").Value),
            };
        }
    }
}
