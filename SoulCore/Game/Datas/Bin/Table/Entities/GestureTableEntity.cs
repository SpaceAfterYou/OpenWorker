﻿using SoulCore.Attributes;
using SoulCore.Extensions;
using SoulCore.Game.Enums;
using System;
using System.IO;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    [BinTable("tb_Gesture")]
    public sealed record GestureTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public Hero Hero { get; }
        public byte MinLevel { get; }
        public byte InitialGesture { get; }
        public uint CoolTime { get; }
        public ushort Unknown5 { get; }
        public uint Unknown6 { get; }
        public string Unknown7 { get; }
        public byte Unknown8 { get; }
        public uint Unknown9 { get; }
        public uint Unknown10 { get; }
        public uint Unknown11 { get; }
        public uint Unknown12 { get; }
        public string Unknown13 { get; }
        public string Unknown14 { get; }
        public string Unknown15 { get; }
        public uint Unknown16 { get; }
        public ushort Unknown17 { get; }
        public ushort Unknown18 { get; }

        internal GestureTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Hero = br.ReadHero();
            MinLevel = br.ReadByte();
            InitialGesture = br.ReadByte();
            CoolTime = br.ReadUInt32();
            Unknown5 = br.ReadUInt16();
            Unknown6 = br.ReadUInt32();
            Unknown7 = br.ReadByteLengthUnicodeString();
            Unknown8 = br.ReadByte();
            Unknown9 = br.ReadUInt32();
            Unknown10 = br.ReadUInt32();
            Unknown11 = br.ReadUInt32();
            Unknown12 = br.ReadUInt32();
            Unknown13 = br.ReadByteLengthUnicodeString();
            Unknown14 = br.ReadByteLengthUnicodeString();
            Unknown15 = br.ReadByteLengthUnicodeString();
            Unknown16 = br.ReadUInt32();
            Unknown17 = br.ReadUInt16();
            Unknown18 = br.ReadUInt16();
        }
    }
}
