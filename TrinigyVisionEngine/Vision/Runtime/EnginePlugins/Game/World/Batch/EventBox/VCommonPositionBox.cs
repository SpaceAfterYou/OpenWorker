using System;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
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
        public int Entity { get; }

        /// <summary>
        ///
        /// </summary>
        public int Group { get; }

        internal VCommonPositionBox(XmlNode xml) : base(xml)
        {
            EntityType = (EntityType)Enum.Parse(typeof(EntityType), xml.SelectSingleNode("m_eEntityType").Attributes.GetNamedItem("value").Value, true);
            Entity = int.Parse(xml.SelectSingleNode("m_iEntityID").Attributes.GetNamedItem("value").Value);
            Group = int.Parse(xml.SelectSingleNode("m_iGroup").Attributes.GetNamedItem("value").Value);
        }
    }
}
