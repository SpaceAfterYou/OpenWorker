using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
{
    public enum ScriptType : byte
    {
        None,
        Spawn,

        [XmlEnum("HP")]
        Hp
    }
}