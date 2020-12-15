using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    public sealed record ItemClassifyTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public byte Unknown5 { get; }
        public ushort Unknown6 { get; }
        public byte Unknown7 { get; }
        public ushort Unknown8 { get; }
        public byte Unknown9 { get; }
        public ushort Unknown10 { get; }
        public byte Unknown11 { get; }
        public ushort Unknown12 { get; }
        public byte Unknown13 { get; }
        public byte Unknown14 { get; }
        public ItemClassifyType Type { get; }
        public byte Unknown16 { get; }
        public byte Unknown17 { get; }
        public byte Unknown18 { get; }
        public byte Unknown19 { get; }
        public ushort Unknown20 { get; }
        public ushort Socket { get; }
        public ushort Unknown22 { get; }
        public string MoveAction { get; }
        public string DropAction { get; }
        public string EquipAction { get; }
        public string UnequipAction { get; }
        public short Unknown27 { get; }

        internal ItemClassifyTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Unknown5 = br.ReadByte();
            Unknown6 = br.ReadUInt16();
            Unknown7 = br.ReadByte();
            Unknown8 = br.ReadUInt16();
            Unknown9 = br.ReadByte();
            Unknown10 = br.ReadUInt16();
            Unknown11 = br.ReadByte();
            Unknown12 = br.ReadUInt16();
            Unknown13 = br.ReadByte();
            Unknown14 = br.ReadByte();
            Type = br.ReadItemClassifyType();
            Unknown16 = br.ReadByte();
            Unknown17 = br.ReadByte();
            Unknown18 = br.ReadByte();
            Unknown19 = br.ReadByte();
            Unknown20 = br.ReadUInt16();
            Socket = br.ReadUInt16();
            Unknown22 = br.ReadUInt16();
            MoveAction = br.ReadByteLengthUnicodeString();
            DropAction = br.ReadByteLengthUnicodeString();
            EquipAction = br.ReadByteLengthUnicodeString();
            UnequipAction = br.ReadByteLengthUnicodeString();
            Unknown27 = br.ReadInt16();
        }
    }
}
