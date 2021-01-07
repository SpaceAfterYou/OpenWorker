using ow.Framework.Attributes;
using ow.Framework.Extensions;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_Pass_Info")]
    public sealed record PassInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public uint v6 { get; }
        public byte v7 { get; }
        public uint v8 { get; }
        public string StartDate { get; }
        public string EndDate { get; }
        public byte v11 { get; }
        public string Cover { get; }
        public string ItemIcon { get; }
        public uint v14 { get; }

        internal PassInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            v6 = br.ReadUInt32();
            v7 = br.ReadByte();
            v8 = br.ReadUInt32();
            StartDate = br.ReadByteLengthUnicodeString();
            EndDate = br.ReadByteLengthUnicodeString();
            v11 = br.ReadByte();
            Cover = br.ReadByteLengthUnicodeString();
            ItemIcon = br.ReadByteLengthUnicodeString();
            v14 = br.ReadUInt32();
        }
    }
}