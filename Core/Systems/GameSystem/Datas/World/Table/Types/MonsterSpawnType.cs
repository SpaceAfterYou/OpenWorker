using System;
using System.Xml.Serialization;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
{
    [Flags]
    public enum MonsterSpawnType
    {
        Monster,
        DestructionObject,
        TreasureBox,
        Unit,

        [XmlEnum("NPC")]
        Npc
    }
}