using System;
using System.Collections.Generic;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
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
        public float EventOperation { get; }

        /// <summary>
        /// The event timeout, ms(1seconds = 1000ms)
        /// </summary>
        public float EventTime { get; }

        /// <summary>
        /// Spawn Box ID
        /// </summary>
        public IReadOnlyList<int> SpawnBox { get; }

        internal VCheckEventSpawnBox(XmlNode xml) : base(xml)
        {
            EventType = (EventType)Enum.Parse(typeof(EventType), xml.SelectSingleNode("m_eEvent_Type").Attributes.GetNamedItem("value").Value, false);
            EventRate = float.Parse(xml.SelectSingleNode("m_fEvent_Rate").Attributes.GetNamedItem("value").Value);
            EventDelayTime = float.Parse(xml.SelectSingleNode("m_fEvent_Delay_Time").Attributes.GetNamedItem("value").Value);
            EventOperation = float.Parse(xml.SelectSingleNode("m_iEvent_Operation_ID").Attributes.GetNamedItem("value").Value);
            EventTime = float.Parse(xml.SelectSingleNode("m_fEvent_Time").Attributes.GetNamedItem("value").Value);
            SpawnBox = new List<int>(5)
            {
                int.Parse(xml.SelectSingleNode("m_iSpawn_Box_ID_0").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iSpawn_Box_ID_1").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iSpawn_Box_ID_2").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iSpawn_Box_ID_3").Attributes.GetNamedItem("value").Value),
                int.Parse(xml.SelectSingleNode("m_iSpawn_Box_ID_4").Attributes.GetNamedItem("value").Value)
            };
        }
    }
}
