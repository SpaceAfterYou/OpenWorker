using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VSafeAreaBox : VEntity
    {
        internal VSafeAreaBox(XmlNode xml) : base(xml)
        {
        }
    }
}
