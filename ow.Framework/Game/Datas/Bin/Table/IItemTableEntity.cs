using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt32;

    public interface IItemTableEntity : ITableEntity<KeyType>
    {
        uint ClassifyId { get; }
        byte Unknown7 { get; }
        byte MaxSlots { get; }
        ushort Unknown9 { get; }
        uint SellPrice { get; }
        uint Unknown11 { get; }
        uint Unknown12 { get; }
        uint Unknown13 { get; }
        ushort MaxStackCount { get; }
        byte Unknown15 { get; }
        uint Unknown16 { get; }
        uint Unknown17 { get; }
        uint Info { get; }
        ushort MinLevel { get; }
        Hero Hero { get; }
        byte Unknown21 { get; }
        byte Unknown22 { get; }
        byte Unknown23 { get; }
        uint CostumeSet { get; }
        string BroochSlots { get; }
        byte Durability { get; }
        byte Unknown27 { get; }
        uint MinAttackDamange { get; }
        uint MaxAttackDamage { get; }
        uint Unknown30 { get; }
        uint MinDefence { get; }
        uint MaxDefence { get; }
        uint Unknown33 { get; }
        byte Unknown34 { get; }
        byte Unknown35 { get; }
        byte Unknown36 { get; }
        byte Unknown37 { get; }
        byte Unknown38 { get; }
        IReadOnlyList<IItemTableEntityStat> Stats { get; }
        uint Unknown49 { get; }
        uint Unknown50 { get; }
        uint Unknown51 { get; }
        ushort Unknown52 { get; }
        uint Unknown53 { get; }
        uint Unknown54 { get; }
        uint Unknown55 { get; }
        ushort Unknown56 { get; }
        uint Unknown57 { get; }
        ushort Unknown58 { get; }
        uint Unknown59 { get; }
        byte Unknown60 { get; }
        ushort Unknown61 { get; }
        uint Unknown62 { get; }
        ushort Unknown63 { get; }
        byte Unknown64 { get; }
        byte Unknown65 { get; }
        uint Unknown66 { get; }
        byte Unknown67 { get; }
        uint Unknown68 { get; }
        byte Unknown69 { get; }
        byte Unknown70 { get; }
        uint Unknown71 { get; }
        uint Package { get; }
    }
}