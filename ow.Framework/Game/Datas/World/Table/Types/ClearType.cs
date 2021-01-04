using System.Xml.Serialization;

namespace ow.Framework.Game.Datas.World.Table.Types
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