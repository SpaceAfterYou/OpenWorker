using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventPoint
{
    public sealed record VWayPoint : VPoint
    {
        internal VWayPoint(XmlNode xml) : base(xml)
        {
        }
    }
}
