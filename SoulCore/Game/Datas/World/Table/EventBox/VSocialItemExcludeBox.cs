using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VSocialItemExcludeBox : VEntity
    {
        internal VSocialItemExcludeBox(XmlNode xml) : base(xml)
        {
        }
    }
}
