using ow.Framework.Game.Datas.World.Table.EventBox;
using ow.Framework.Game.Datas.World.Table.EventPoint;
using System;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table
{
    public sealed record VRoot
    {
        public VEventBox EventBox { get; }
        public VEventPoint EventPoint { get; }

        internal VRoot(XmlNode xml)
        {
            EventBox = new(xml.SelectSingleNode("Batchs [@eventtype='EventBox']") ?? throw new ApplicationException());
            EventPoint = new(xml.SelectSingleNode("Batchs [@eventtype='EventPoint']") ?? throw new ApplicationException());
        }
    }
}