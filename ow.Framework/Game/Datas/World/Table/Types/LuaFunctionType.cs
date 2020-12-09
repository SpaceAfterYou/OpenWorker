using System;
using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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
