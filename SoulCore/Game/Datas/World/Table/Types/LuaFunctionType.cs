using System.Xml.Serialization;

namespace SoulCore.Game.Datas.World.Table.Types
{
    public enum LuaFunctionType : byte
    {
        Self,
        Party,
        Monster,
        Warp,

        [XmlEnum("NPC")]
        Npc
    }
}
