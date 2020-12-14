using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = Hero;

    public sealed record ClassSelectInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public string Unknown5 { get; }
        public string Unknown6 { get; }
        public uint Unknown7 { get; }
        public uint Unknown8 { get; }
        public uint Unknown9 { get; }
        public uint Unknown10 { get; }
        public uint Unknown11 { get; }
        public uint HandsFashionId { get; }
        public uint Unknown13 { get; }
        public uint OuterwearFashionId { get; }
        public uint Unknown15 { get; }
        public uint StockingsFashionId { get; }
        public uint ShoesFashionId { get; }
        public uint Unknown18 { get; }
        public uint Unknown19 { get; }
        public ushort Unknown20 { get; }
        public ushort Unknown21 { get; }
        public int Unknown22 { get; }
        public ushort Unknown23 { get; }
        public string Unknown24 { get; }
        public string Unknown25 { get; }
        public string Unknown26 { get; }
        public string Unknown27 { get; }
        public string Unknown28 { get; }
        public string Unknown29 { get; }

        internal ClassSelectInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadHero();
            Unknown5 = br.ReadByteLengthUnicodeString();
            Unknown6 = br.ReadByteLengthUnicodeString();
            Unknown7 = br.ReadUInt32();
            Unknown8 = br.ReadUInt32();
            Unknown9 = br.ReadUInt32();
            Unknown10 = br.ReadUInt32();
            Unknown11 = br.ReadUInt32();
            HandsFashionId = br.ReadUInt32();
            Unknown13 = br.ReadUInt32();
            OuterwearFashionId = br.ReadUInt32();
            Unknown15 = br.ReadUInt32();
            StockingsFashionId = br.ReadUInt32();
            ShoesFashionId = br.ReadUInt32();
            Unknown18 = br.ReadUInt32();
            Unknown19 = br.ReadUInt32();
            Unknown20 = br.ReadUInt16();
            Unknown21 = br.ReadUInt16();
            Unknown22 = br.ReadInt32();
            Unknown23 = br.ReadUInt16();
            Unknown24 = br.ReadByteLengthUnicodeString();
            Unknown25 = br.ReadByteLengthUnicodeString();
            Unknown26 = br.ReadByteLengthUnicodeString();
            Unknown27 = br.ReadByteLengthUnicodeString();
            Unknown28 = br.ReadByteLengthUnicodeString();
            Unknown29 = br.ReadByteLengthUnicodeString();
        }
    }
}