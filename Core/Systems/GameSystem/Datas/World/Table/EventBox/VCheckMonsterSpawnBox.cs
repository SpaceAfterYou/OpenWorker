using Core.Systems.GameSystem.Datas.World.Table.Types;
using Core.Systems.GameSystem.Extensions;
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
            Type = xml.GetEnum<MonsterType>("m_eType");
            LoopCount = xml.GetUInt32("m_iLoopCount");
            EntityId = xml.GetUInt32("m_iEntityID");
            CheckBox = Enumerable.Range(0, 10).Select(id => xml.GetUInt32($"m_iCheckBox_{id}")).ToArray();
        }
    }
}