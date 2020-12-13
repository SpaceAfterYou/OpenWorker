using System;
using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
{
    [Flags]
    public enum ScriptType
    {
        None,
        Spawn,

        [XmlEnum("HP")]
        Hp
    }
}