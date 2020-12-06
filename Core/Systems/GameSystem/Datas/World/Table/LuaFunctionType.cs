using System;
using System.Xml.Serialization;

namespace Core.Systems.GameSystem.Datas.World.Table
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