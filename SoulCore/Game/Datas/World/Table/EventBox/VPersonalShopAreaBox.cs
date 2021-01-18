using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VPersonalShopAreaBox : VEntity
    {
        internal VPersonalShopAreaBox(XmlNode xml) : base(xml)
        {
        }
    }
}
