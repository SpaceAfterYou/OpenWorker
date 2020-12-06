using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
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
        public uint LoopCount { get; }

        /// <summary>
        /// Target ID to work with this box to collide (as CheckType and find the target in combination)
        /// </summary>
        public uint EntityId { get; }

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<uint> CheckBox { get; }

        internal VCheckMonsterSpawnBox(XmlNode xml) : base(xml)
        {
            Type = (MonsterType)Enum.Parse(typeof(MonsterType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            LoopCount = uint.Parse(xml.SelectSingleNode("m_iLoopCount").Attributes.GetNamedItem("value").Value);
            EntityId = uint.Parse(xml.SelectSingleNode("m_iEntityID").Attributes.GetNamedItem("value").Value);
            CheckBox = Enumerable.Range(1, 10).Select(id => uint.Parse(xml.SelectSingleNode($"m_iCheckBox_{id}").Attributes.GetNamedItem("value").Value)).ToArray();
        }
    }
}
