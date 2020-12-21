using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VCommonPositionBox : VEntity
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
