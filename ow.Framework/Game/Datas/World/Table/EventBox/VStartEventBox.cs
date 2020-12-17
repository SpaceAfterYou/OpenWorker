using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VStartEventBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public SpawnType SpawnType { get; }

        internal VStartEventBox(XmlNode xml) : base(xml) =>
            SpawnType = xml.GetEnum<SpawnType>("m_eSpawnType");
    }
}
