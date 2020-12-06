using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VCheckSectorBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint CheckSector { get; }

        /// <summary>
        ///
        /// </summary>
        public string Lua { get; }

        /// <summary>
        ///
        /// </summary>
        public uint CheckGate { get; }

        internal VCheckSectorBox(XmlNode xml) : base(xml)
        {
            CheckSector = uint.Parse(xml.SelectSingleNode("m_iCheckSector").Attributes.GetNamedItem("value").Value);
            Lua = xml.SelectSingleNode("m_szLua").Attributes.GetNamedItem("value").Value;
            CheckGate = uint.Parse(xml.SelectSingleNode("m_iCheckGate").Attributes.GetNamedItem("value").Value);
        }
    }
}
