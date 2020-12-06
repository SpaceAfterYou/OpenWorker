using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VMazeEscapeBox : VEntity
    {
        /// <summary>
        /// Field ID
        /// </summary>
        public uint Field { get; }

        /// <summary>
        /// ID of Event Object.
        /// </summary>
        public uint EventObject { get; }

        internal VMazeEscapeBox(XmlNode xml) : base(xml)
        {
            Field = uint.Parse(xml.SelectSingleNode("m_iField").Attributes.GetNamedItem("value").Value);
            EventObject = uint.Parse(xml.SelectSingleNode("m_iEventObject").Attributes.GetNamedItem("value").Value);
        }
    }
}
