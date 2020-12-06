using System;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
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
        public uint NextSector { get; }

        /// <summary>
        ///
        /// </summary>
        public uint Sector { get; }

        internal VServerGateBox(XmlNode xml) : base(xml)
        {
            Type = (GateType)Enum.Parse(typeof(GateType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, true);
            NextSector = uint.Parse(xml.SelectSingleNode("m_iNextSectorID").Attributes.GetNamedItem("value").Value);
            Sector = uint.Parse(xml.SelectSingleNode("m_iSectorID").Attributes.GetNamedItem("value").Value);
        }
    }
}
