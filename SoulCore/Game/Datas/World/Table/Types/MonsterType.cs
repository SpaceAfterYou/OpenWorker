using System.Xml.Serialization;

namespace SoulCore.Game.Datas.World.Table.Types
{
    public enum MonsterType : byte
    {
        Npc,
        Monster,
        DestructionObject,

        [XmlEnum("PC")]
        Pc,
    }
}
