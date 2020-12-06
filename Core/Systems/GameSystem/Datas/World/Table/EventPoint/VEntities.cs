using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventPoint
{
    public sealed class VEntities
    {
        public IReadOnlyList<VWayPoint> WayPoints { get; }
        public IReadOnlyList<VEscortPoint> EscortPoints { get; }

        internal VEntities(XmlNode xml)
        {
            WayPoints = xml.SelectNodes("VWayPoint").Cast<XmlNode>().Select(v => new VWayPoint(v)).ToArray();
            EscortPoints = xml.SelectNodes("VEscortPoint").Cast<XmlNode>().Select(v => new VEscortPoint(v)).ToArray();
        }
    }
}
