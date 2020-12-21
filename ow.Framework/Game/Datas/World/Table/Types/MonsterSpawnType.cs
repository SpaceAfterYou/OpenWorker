using System;
using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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
