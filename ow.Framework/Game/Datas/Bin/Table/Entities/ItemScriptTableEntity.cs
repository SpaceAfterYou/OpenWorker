using ow.Framework.Attributes;
using ow.Framework.Extensions;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_item_script")]
    public sealed record ItemScriptTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public string Icon { get; }
        public string Unknown6 { get; }
        public string Unknown7 { get; }
        public string Unknown8 { get; }
        public string Unknown9 { get; }
        public string Unknown10 { get; }
        public byte Unknown11 { get; }
        public byte Unknown12 { get; }
        public byte Unknown13 { get; }
        public byte Unknown14 { get; }
        public byte Unknown15 { get; }
        public string Name { get; }
        public string Description { get; }

        internal ItemScriptTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Icon = br.ReadByteLengthUnicodeString();
            Unknown6 = br.ReadByteLengthUnicodeString();
            Unknown7 = br.ReadByteLengthUnicodeString();
            Unknown8 = br.ReadByteLengthUnicodeString();
            Unknown9 = br.ReadByteLengthUnicodeString();
            Unknown10 = br.ReadByteLengthUnicodeString();
            Unknown11 = br.ReadByte();
            Unknown12 = br.ReadByte();
            Unknown13 = br.ReadByte();
            Unknown14 = br.ReadByte();
            Unknown15 = br.ReadByte();
            Name = br.ReadByteLengthUnicodeString();
            Description = br.ReadByteLengthUnicodeString();
        }
    }
}