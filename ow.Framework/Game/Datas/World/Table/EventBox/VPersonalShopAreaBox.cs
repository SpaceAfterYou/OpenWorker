using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VPersonalShopAreaBox : VEntity
    {
        internal VPersonalShopAreaBox(XmlNode xml) : base(xml)
        {
        }
    }
}
