using System;
using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
{
    [Flags]
    public enum ClearType
    {
        None,
        Kill,
        Always,
        ModeClear,

        [XmlEnum("KillPerpect")]
        Kill_Perpect,
    }
}