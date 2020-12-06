using System;
using System.Xml.Serialization;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
{
    [Flags]
    public enum MonsterType
    {
        Npc,
        Monster,
        DestructionObject,

        [XmlEnum("PC")]
        Pc,
    }
}
