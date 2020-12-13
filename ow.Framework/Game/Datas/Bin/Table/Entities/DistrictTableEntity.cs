using ow.Framework.Extensions;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    internal sealed class DistrictTableEntity : IDistrictTableEntity
    {
        public KeyType Id { get; }
        public ushort Unknown5 { get; }
        public ushort Unknown6 { get; }
        public ushort Unknown7 { get; }
        public string Unknown8 { get; }
        public string Batch { get; }
        public ushort Unknown10 { get; }
        public uint Unknown11 { get; }
        public uint Unknown12 { get; }
        public string Bgm { get; }
        public string Bg { get; }
        public byte Unknown15 { get; }
        public byte Unknown16 { get; }
        public string Map { get; }
        public byte Unknown18 { get; }
        public byte Unknown19 { get; }

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