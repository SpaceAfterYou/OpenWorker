using Core.Systems.GameSystem.Datas.World.Table.Types;
using Core.Systems.GameSystem.Extensions;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VStartEventBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public SpawnType SpawnType { get; }

        internal VStartEventBox(XmlNode xml) : base(xml) =>
            SpawnType = xml.GetEnum<SpawnType>("m_eSpawnType");
    }
}