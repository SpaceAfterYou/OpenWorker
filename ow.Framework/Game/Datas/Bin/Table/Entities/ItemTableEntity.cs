using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    public sealed record ItemTableEntity : ITableEntity<KeyType>
    {
        public readonly struct Stat
        {
            public uint Id { get; }
            public int Value { get; }

            internal Stat(uint id, int value) => (Id, Value) = (id, value);
        }

        public KeyType Id { get; }
        public uint Classify { get; }
        public byte Unknown7 { get; }
        public byte MaxSlots { get; }
        public ushort Unknown9 { get; }
        public uint SellPrice { get; }
        public uint Unknown11 { get; }
        public uint Unknown12 { get; }
        public uint Unknown13 { get; }
        public ushort MaxStackCount { get; }
        public byte Unknown15 { get; }
        public uint Unknown16 { get; }
        public uint Unknown17 { get; }
        public uint Info { get; }
        public ushort MinLevel { get; }
        public Hero Hero { get; }
        public byte Unknown21 { get; }
        public byte Unknown22 { get; }
        public byte Unknown23 { get; }
        public uint CostumeSet { get; }
        public string BroochSlots { get; }
        public byte Durability { get; }
        public byte Unknown27 { get; }
        public uint MinAttackDamange { get; }
        public uint MaxAttackDamage { get; }
        public uint Unknown30 { get; }
        public uint MinDefence { get; }
        public uint MaxDefence { get; }
        public uint Unknown33 { get; }
        public byte Unknown34 { get; }
        public byte Unknown35 { get; }
        public byte Unknown36 { get; }
        public byte Unknown37 { get; }
        public byte Unknown38 { get; }
        public IReadOnlyList<Stat> Stats { get; }
        public uint Unknown49 { get; }
        public uint Unknown50 { get; }
        public uint Unknown51 { get; }
        public ushort Unknown52 { get; }
        public uint Unknown53 { get; }
        public uint Unknown54 { get; }
        public uint Unknown55 { get; }
        public ushort Unknown56 { get; }
        public uint Unknown57 { get; }
        public ushort Unknown58 { get; }
        public uint Unknown59 { get; }
        public byte Unknown60 { get; }
        public ushort Unknown61 { get; }
        public uint Unknown62 { get; }
        public ushort Unknown63 { get; }
        public byte Unknown64 { get; }
        public byte Unknown65 { get; }
        public uint Unknown66 { get; }
        public byte Unknown67 { get; }
        public uint Unknown68 { get; }
        public byte Unknown69 { get; }
        public byte Unknown70 { get; }
        public uint Unknown71 { get; }
        public uint Package { get; }

        internal ItemTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Classify = br.ReadUInt32();
            Unknown7 = br.ReadByte();
            MaxSlots = br.ReadByte();
            Unknown9 = br.ReadUInt16();
            SellPrice = br.ReadUInt32();
            Unknown11 = br.ReadUInt32();
            Unknown12 = br.ReadUInt32();
            Unknown13 = br.ReadUInt32();
            MaxStackCount = br.ReadUInt16();
            Unknown15 = br.ReadByte();
            Unknown16 = br.ReadUInt32();
            Unknown17 = br.ReadUInt32();
            Info = br.ReadUInt32();
            MinLevel = br.ReadUInt16();
            Hero = br.ReadHero();
            Unknown21 = br.ReadByte();
            Unknown22 = br.ReadByte();
            Unknown23 = br.ReadByte();
            CostumeSet = br.ReadUInt32();
            BroochSlots = br.ReadByteLengthUnicodeString();
            Durability = br.ReadByte();
            Unknown27 = br.ReadByte();
            MinAttackDamange = br.ReadUInt32();
            MaxAttackDamage = br.ReadUInt32();
            Unknown30 = br.ReadUInt32();
            MinDefence = br.ReadUInt32();
            MaxDefence = br.ReadUInt32();
            Unknown33 = br.ReadUInt32();
            Unknown34 = br.ReadByte();
            Unknown35 = br.ReadByte();
            Unknown36 = br.ReadByte();
            Unknown37 = br.ReadByte();
            Unknown38 = br.ReadByte();
            Stats = ReadStats(br);
            Unknown49 = br.ReadUInt32();
            Unknown50 = br.ReadUInt32();
            Unknown51 = br.ReadUInt32();
            Unknown52 = br.ReadUInt16();
            Unknown53 = br.ReadUInt32();
            Unknown54 = br.ReadUInt32();
            Unknown55 = br.ReadUInt32();
            Unknown56 = br.ReadUInt16();
            Unknown57 = br.ReadUInt32();
            Unknown58 = br.ReadUInt16();
            Unknown59 = br.ReadUInt32();
            Unknown60 = br.ReadByte();
            Unknown61 = br.ReadUInt16();
            Unknown62 = br.ReadUInt32();
            Unknown63 = br.ReadUInt16();
            Unknown64 = br.ReadByte();
            Unknown65 = br.ReadByte();
            Unknown66 = br.ReadUInt32();
            Unknown67 = br.ReadByte();
            Unknown68 = br.ReadUInt32();
            Unknown69 = br.ReadByte();
            Unknown70 = br.ReadByte();
            Unknown71 = br.ReadUInt32();
            Package = br.ReadUInt32();
        }

        private static Stat[] ReadStats(BinaryReader br) => Enumerable
            .Repeat(0, Defines.StatsPerItem)
            .Select(_ => br.ReadUInt32())
            .Select(id => new Stat(id, br.ReadInt32()))
            .ToArray();
    }
}