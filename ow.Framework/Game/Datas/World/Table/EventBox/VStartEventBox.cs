using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.Game.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
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
