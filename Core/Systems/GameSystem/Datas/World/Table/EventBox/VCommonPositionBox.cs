using Core.Systems.GameSystem.Datas.World.Table.Types;
using Core.Systems.GameSystem.Extensions;
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
            EntityType = xml.GetEnum<EntityType>("m_eEntityType");
            Entity = xml.GetUInt32("m_iEntityID");
            Group = xml.GetUInt32("m_iGroup");
        }
    }
}