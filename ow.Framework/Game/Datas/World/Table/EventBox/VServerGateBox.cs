using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.Game.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
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
            Type = xml.GetEnum<GateType>("m_eType");
            NextSector = xml.GetUInt32("m_iNextSectorID");
            Sector = xml.GetUInt32("m_iSectorID");
        }
    }
}