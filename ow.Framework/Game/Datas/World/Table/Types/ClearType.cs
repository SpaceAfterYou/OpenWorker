using System;
using System.Xml.Serialization;

namespace ow.Framework.FS.Datas.World.Table.Types
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