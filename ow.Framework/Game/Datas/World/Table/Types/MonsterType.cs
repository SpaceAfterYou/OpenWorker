using System;
using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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
