using ow.Framework.Extensions;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    public sealed class DistrictTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; init; }
        public ushort Unknown5 { get; init; }
        public ushort Unknown6 { get; init; }
        public ushort Unknown7 { get; init; }
        public string Unknown8 { get; init; }
        public string Batch { get; init; }
        public ushort Unknown10 { get; init; }
        public uint Unknown11 { get; init; }
        public uint Unknown12 { get; init; }
        public string Bgm { get; init; }
        public string Bg { get; init; }
        public byte Unknown15 { get; init; }
        public byte Unknown16 { get; init; }
        public string Map { get; init; }
        public byte Unknown18 { get; init; }
        public byte Unknown19 { get; init; }

        internal DistrictTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Unknown5 = br.ReadUInt16();
            Unknown6 = br.ReadUInt16();
            Unknown7 = br.ReadUInt16();
            Unknown8 = br.ReadByteLengthUnicodeString();
            Batch = br.ReadByteLengthUnicodeString();
            Unknown10 = br.ReadUInt16();
            Unknown11 = br.ReadUInt32();
            Unknown12 = br.ReadUInt32();
            Bgm = br.ReadByteLengthUnicodeString();
            Bg = br.ReadByteLengthUnicodeString();
            Unknown15 = br.ReadByte();
            Unknown16 = br.ReadByte();
            Map = br.ReadByteLengthUnicodeString();
            Unknown18 = br.ReadByte();
            Unknown19 = br.ReadByte();
        }
    }
}
