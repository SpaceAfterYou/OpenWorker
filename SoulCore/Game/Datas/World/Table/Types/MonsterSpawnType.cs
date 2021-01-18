using System.Xml.Serialization;

namespace SoulCore.Game.Datas.World.Table.Types
{
    public enum MonsterSpawnType : byte
    {
        Monster,
        DestructionObject,
        TreasureBox,
        Unit,

        [XmlEnum("NPC")]
        Npc
    }
}
