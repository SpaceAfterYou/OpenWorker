using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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