using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VSocialItemExcludeBox : VEntity
    {
        internal VSocialItemExcludeBox(XmlNode xml) : base(xml)
        {
        }
    }
}
