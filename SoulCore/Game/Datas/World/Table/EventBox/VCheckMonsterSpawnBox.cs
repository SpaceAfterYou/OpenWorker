using SoulCore.Game.Datas.World.Table.Types;
using SoulCore.IO.File.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VCheckMonsterSpawnBox : VEntity
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
        public uint Entity { get; }

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<uint> CheckBoxes { get; }

        internal VCheckMonsterSpawnBox(XmlNode xml) : base(xml)
        {
            Type = xml.GetEnum<MonsterType>("m_eType");
            LoopCount = xml.GetUInt32("m_iLoopCount");
            Entity = xml.GetUInt32("m_iEntityID");
            CheckBoxes = Enumerable.Range(0, 10).Select(id => xml.GetUInt32($"m_iCheckBox_{id}")).ToArray();
        }
    }
}
