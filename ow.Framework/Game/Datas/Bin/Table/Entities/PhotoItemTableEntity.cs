using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    public sealed record PhotoItemTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public uint Unknown1 { get; }
        public ushort Unknown2 { get; }
        public string Unknown3 { get; }
        public string Unknown4 { get; }
        public string Unknown5 { get; }
        public string Unknown6 { get; }
        public string Unknown7 { get; }
        public string Unknown8 { get; }
        public string Unknown9 { get; }
        public string Unknown10 { get; }
        public string Unknown11 { get; }
        public string Unknown12 { get; }
        public Hero Hero { get; }
        public byte Unknown14 { get; }

        internal PhotoItemTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt16();
            Unknown3 = br.ReadByteLengthUnicodeString();
            Unknown4 = br.ReadByteLengthUnicodeString();
            Unknown5 = br.ReadByteLengthUnicodeString();
            Unknown6 = br.ReadByteLengthUnicodeString();
            Unknown7 = br.ReadByteLengthUnicodeString();
            Unknown8 = br.ReadByteLengthUnicodeString();
            Unknown9 = br.ReadByteLengthUnicodeString();
            Unknown10 = br.ReadByteLengthUnicodeString();
            Unknown11 = br.ReadByteLengthUnicodeString();
            Unknown12 = br.ReadByteLengthUnicodeString();
            Hero = br.ReadHero();
            Unknown14 = br.ReadByte();
        }
    }
}

// https://youtu.be/l74Ot_2kuNs