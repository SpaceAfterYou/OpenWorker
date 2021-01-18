using System.Xml.Serialization;

namespace SoulCore.Game.Datas.World.Table.Types
{
    public enum ClearType : byte
    {
        None,
        Kill,
        Always,
        ModeClear,

        [XmlEnum("KillPerpect")]
        Kill_Perpect,
    }
}
