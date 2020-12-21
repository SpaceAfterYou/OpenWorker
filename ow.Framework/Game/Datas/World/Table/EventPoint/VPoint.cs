using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventPoint
{
    public abstract record VPoint : VEntity
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
        public IReadOnlyList<uint> NextPoints { get; }

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
            Type = xml.GetEnum<PointType>("m_eType");
            BattleType = xml.GetEnum<BattleType>("m_eBattleType");
            BeforePoint = xml.GetUInt32("m_iBeforePoint");
            NextPoints = Enumerable.Range(1, 4).Select(id => xml.GetUInt32($"m_iNextPoint{(id > 1 ? id : "")}")).ToArray();
            IdleAction = xml.GetString("m_szIdleAction");
            IdleActionRatio = xml.GetUInt32("m_uiIdleActionRatio");
            DelayTime = xml.GetUInt32("m_uiDelayTime");
            RepeatCount = xml.GetByte("m_RepeatCount");
        }
    }
}
