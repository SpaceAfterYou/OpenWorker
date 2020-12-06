using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VCheckEventSpawnBox : VEntity
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
        public float EventOperationId { get; }

        /// <summary>
        /// The event timeout, ms(1seconds = 1000ms)
        /// </summary>
        public float EventTime { get; }

        /// <summary>
        /// Spawn Box ID
        /// </summary>
        public IReadOnlyList<uint> SpawnBoxId { get; }

        internal VCheckEventSpawnBox(XmlNode xml) : base(xml)
        {
            EventType = (EventType)Enum.Parse(typeof(EventType), xml.SelectSingleNode("m_eEvent_Type").Attributes.GetNamedItem("value").Value, false);
            EventRate = float.Parse(xml.SelectSingleNode("m_fEvent_Rate").Attributes.GetNamedItem("value").Value);
            EventDelayTime = float.Parse(xml.SelectSingleNode("m_fEvent_Delay_Time").Attributes.GetNamedItem("value").Value);
            EventOperationId = uint.Parse(xml.SelectSingleNode("m_iEvent_Operation_ID").Attributes.GetNamedItem("value").Value);
            EventTime = float.Parse(xml.SelectSingleNode("m_fEvent_Time").Attributes.GetNamedItem("value").Value);
            SpawnBoxId = Enumerable.Range(1, 5).Select(id => uint.Parse(xml.SelectSingleNode($"m_iSpawn_Box_ID_{id}").Attributes.GetNamedItem("value").Value)).ToArray();
        }
    }
}
