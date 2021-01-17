using ow.Framework.Attributes;
using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_Item_Classify")]
    public sealed record ItemClassifyTableEntity : ITableEntity<KeyType>
    {
        public struct ActionInfo
        {
            public string Move { get; }
            public string Drop { get; }
            public string Equip { get; }
            public string Unequip { get; }

            internal ActionInfo(BinaryReader br)
            {
                Move = br.ReadByteLengthUnicodeString();
                Drop = br.ReadByteLengthUnicodeString();
                Equip = br.ReadByteLengthUnicodeString();
                Unequip = br.ReadByteLengthUnicodeString();
            }
        }

        public KeyType Id { get; }
        public byte Group { get; }
        public ushort Unknown6 { get; }
        public byte SubGroup { get; }
        public ushort Unknown8 { get; }
        public byte Category { get; }
        public ushort Unknown10 { get; }
        public byte SubCategory { get; }
        public ushort Unknown12 { get; }
        public byte GainType { get; }
        public ItemClassifyInventoryType InventoryType { get; }
        public ItemClassifySlotType SlotType { get; }
        public byte RepairType { get; }
        public byte UseState { get; }
        public byte UseType { get; }
        public byte ConsumeType { get; }
        public ushort Unknown20 { get; }
        public ushort SocketId { get; }
        public ushort Unknown22 { get; }
        public ActionInfo Action { get; }
        public short Unknown27 { get; }

        internal ItemClassifyTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Group = br.ReadByte();
            Unknown6 = br.ReadUInt16();
            SubGroup = br.ReadByte();
            Unknown8 = br.ReadUInt16();
            Category = br.ReadByte();
            Unknown10 = br.ReadUInt16();
            SubCategory = br.ReadByte();
            Unknown12 = br.ReadUInt16();
            GainType = br.ReadByte();
            InventoryType = br.ReadItemClassifyInventoryType();
            SlotType = br.ReadItemClassifySlotType();
            RepairType = br.ReadByte();
            UseState = br.ReadByte();
            UseType = br.ReadByte();
            ConsumeType = br.ReadByte();
            Unknown20 = br.ReadUInt16();
            SocketId = br.ReadUInt16();
            Unknown22 = br.ReadUInt16();
            Action = new(br);
            Unknown27 = br.ReadInt16();
        }
    }
}