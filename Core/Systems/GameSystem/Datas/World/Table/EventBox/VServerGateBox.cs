using Core.Systems.GameSystem.Datas.World.Table.Types;
using Core.Systems.GameSystem.Extensions;
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
        public uint NextSectorId { get; }

        /// <summary>
        ///
        /// </summary>
        public uint SectorId { get; }

        internal VServerGateBox(XmlNode xml) : base(xml)
        {
            Type = xml.GetEnum<GateType>("m_eType");
            NextSectorId = xml.GetUInt32("m_iNextSectorID");
            SectorId = xml.GetUInt32("m_iSectorID");
        }
    }
}