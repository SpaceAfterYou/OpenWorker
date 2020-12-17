using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VCheckEventSpawnBox : VEntity
    {
        /// <summary>
        /// Object Type
        /// </summary>
        public EventType EventType { get; }

        /// <summary>
        /// Generated event probability, ten thousand minutes rate(10000=100%)
        /// </summary>
        public float EventRate { get; }

        /// <summary>
        /// Until the event occurs after the delay time
        /// </summary>
        public float EventDelayTime { get; }

        /// <summary>
        /// Operation of the event file
        /// </summary>
        public uint EventOperation { get; }

        /// <summary>
        /// The event timeout, ms(1seconds = 1000ms)
        /// </summary>
        public float EventTime { get; }

        /// <summary>
        /// Spawn Box ID
        /// </summary>
        public IReadOnlyList<uint> SpawnBoxes { get; }

        internal VCheckEventSpawnBox(XmlNode xml) : base(xml)
        {
            EventType = xml.GetEnum<EventType>("m_eEvent_Type");
            EventRate = xml.GetSingle("m_fEvent_Rate");
            EventDelayTime = xml.GetSingle("m_fEvent_Delay_Time");
            EventOperation = xml.GetUInt32("m_iEvent_Operation_ID");
            EventTime = xml.GetSingle("m_fEvent_Time");
            SpawnBoxes = Enumerable.Range(1, 6).Select(id => xml.GetUInt32($"m_iSpawn_Box_ID_{id}")).ToArray();
        }
    }
}
