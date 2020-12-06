using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VInterActionBox : VEntity
    {
        /// <summary>
        /// Interactive table ID
        /// </summary>
        public uint Interaction { get; }

        /// <summary>
        /// Interactive ObjectKey
        /// </summary>
        public string ObjectKey { get; }

        internal VInterActionBox(XmlNode xml) : base(xml)
        {
            Interaction = uint.Parse(xml.SelectSingleNode("m_iInteractionID").Attributes.GetNamedItem("value").Value);
            ObjectKey = xml.SelectSingleNode("m_sObjectKey").Attributes.GetNamedItem("value").Value;
        }
    }
}
