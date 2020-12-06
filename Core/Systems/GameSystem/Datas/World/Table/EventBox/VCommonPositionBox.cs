using System;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VCommonPositionBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public EntityType EntityType { get; }

        /// <summary>
        ///
        /// </summary>
        public uint Entity { get; }

        /// <summary>
        ///
        /// </summary>
        public uint Group { get; }

        internal VCommonPositionBox(XmlNode xml) : base(xml)
        {
            EntityType = (EntityType)Enum.Parse(typeof(EntityType), xml.SelectSingleNode("m_eEntityType").Attributes.GetNamedItem("value").Value, true);
            Entity = uint.Parse(xml.SelectSingleNode("m_iEntityID").Attributes.GetNamedItem("value").Value);
            Group = uint.Parse(xml.SelectSingleNode("m_iGroup").Attributes.GetNamedItem("value").Value);
        }
    }
}
