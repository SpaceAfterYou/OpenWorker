using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventPoint
{
    public sealed record VEventPoint
    {
        public IReadOnlyList<VWayPoint> WayPoints { get; }
        public IReadOnlyList<VEscortPoint> EscortPoints { get; }

        internal VEventPoint(XmlNode xml)
        {
            WayPoints = xml.SelectNodes("VWayPoint")?.Cast<XmlNode>().Select(v => new VWayPoint(v)).ToArray() ?? Array.Empty<VWayPoint>();
            EscortPoints = xml.SelectNodes("VEscortPoint")?.Cast<XmlNode>().Select(v => new VEscortPoint(v)).ToArray() ?? Array.Empty<VEscortPoint>();
        }
    }
}
