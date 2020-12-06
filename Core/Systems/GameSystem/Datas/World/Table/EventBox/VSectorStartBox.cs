using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VSectorStartBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint Sector { get; }

        internal VSectorStartBox(XmlNode xml) : base(xml)
        {
            Sector = uint.Parse(xml.SelectSingleNode("m_nSectorID").Attributes.GetNamedItem("value").Value);
        }
    }
}
