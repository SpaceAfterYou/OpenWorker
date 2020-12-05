using System;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VServerGateBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public GateType Type { get; }

        /// <summary>
        ///
        /// </summary>
        public int NextSector { get; }

        /// <summary>
        ///
        /// </summary>
        public int Sector { get; }

        internal VServerGateBox(XmlNode xml) : base(xml)
        {
            Type = (GateType)Enum.Parse(typeof(GateType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            NextSector = int.Parse(xml.SelectSingleNode("m_iNextSectorID").Attributes.GetNamedItem("value").Value);
            Sector = int.Parse(xml.SelectSingleNode("m_iSectorID").Attributes.GetNamedItem("value").Value);
        }
    }
}
