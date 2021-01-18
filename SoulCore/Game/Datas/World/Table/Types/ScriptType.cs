using System.Xml.Serialization;

namespace SoulCore.Game.Datas.World.Table.Types
{
    public enum ScriptType : byte
    {
        None,
        Spawn,

        [XmlEnum("HP")]
        Hp
    }
}
