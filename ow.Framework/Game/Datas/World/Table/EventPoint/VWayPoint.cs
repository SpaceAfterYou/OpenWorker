using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventPoint
{
    public sealed record VWayPoint : VPoint
    {
        internal VWayPoint(XmlNode xml) : base(xml)
        {
        }
    }
}
