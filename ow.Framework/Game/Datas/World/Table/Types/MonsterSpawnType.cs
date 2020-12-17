using System;
using System.Xml.Serialization;

namespace ow.Framework.FS.Datas.World.Table.Types
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
