using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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