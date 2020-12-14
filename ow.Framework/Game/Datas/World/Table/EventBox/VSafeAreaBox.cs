using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VSafeAreaBox : VEntity
    {
        internal VSafeAreaBox(XmlNode xml) : base(xml)
        {
        }
    }
}
