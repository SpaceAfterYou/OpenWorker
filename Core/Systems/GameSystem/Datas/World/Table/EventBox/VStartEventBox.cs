using System;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VStartEventBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public SpawnType SpawnType { get; }

        internal VStartEventBox(XmlNode xml) : base(xml)
        {
            SpawnType = (SpawnType)Enum.Parse(typeof(SpawnType), xml.SelectSingleNode("m_eSpawnType").Attributes.GetNamedItem("value").Value, true);
        }
    }
}
