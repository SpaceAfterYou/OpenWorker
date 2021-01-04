using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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