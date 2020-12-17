using System;
using System.Xml.Serialization;

namespace ow.Framework.FS.Datas.World.Table.Types
{
    [Flags]
    public enum LuaFunctionType
    {
        Self,
        Party,
        Monster,
        Warp,

        [XmlEnum("NPC")]
        Npc
    }
}
