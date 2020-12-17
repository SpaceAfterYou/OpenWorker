using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    public sealed record BoosterTableEntity : ITableEntity<KeyType>
    {
        public readonly struct Stat
        {
            public ushort Id { get; }
            public float Value { get; }

            internal Stat(ushort id, float value) => (Id, Value) = (id, value);
        }

        public KeyType Id { get; }
        public ushort Unknown7 { get; }
        public byte Unknown8 { get; }
        public byte Unknown9 { get; }
        public byte Unknown10 { get; }
        public byte Unknown11 { get; }
        public byte Unknown12 { get; }
        public byte Unknown13 { get; }
        public byte Unknown14 { get; }
        public byte Unknown15 { get; }
        public byte Unknown16 { get; }
        public byte Unknown17 { get; }
        public byte Unknown18 { get; }
        public byte Unknown19 { get; }
        public byte Unknown20 { get; }
        public byte Unknown21 { get; }
        public byte Unknown22 { get; }
        public byte Unknown23 { get; }
        public IReadOnlyList<Stat> Stats { get; }
        public float Unknown24 { get; }
        public float Unknown25 { get; }
        public float Unknown26 { get; }
        public float Unknown27 { get; }
        public float Unknown28 { get; }
        public float Unknown29 { get; }
        public float Unknown30 { get; }
        public float Unknown31 { get; }
        public ushort Unknown32 { get; }
        public ushort Unknown33 { get; }
        public ushort Unknown34 { get; }
        public ushort Unknown35 { get; }
        public ushort Unknown36 { get; }
        public ushort Unknown37 { get; }
        public ushort Unknown38 { get; }
        public ushort Unknown39 { get; }
        public ushort Unknown40 { get; }
        public byte Unknown41 { get; }
        public uint Unknown42 { get; }

        internal BoosterTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Unknown7 = br.ReadUInt16();
            Unknown8 = br.ReadByte();
            Unknown9 = br.ReadByte();
            Unknown10 = br.ReadByte();
            Unknown11 = br.ReadByte();
            Unknown12 = br.ReadByte();
            Unknown13 = br.ReadByte();
            Unknown14 = br.ReadByte();
            Unknown15 = br.ReadByte();
            Unknown16 = br.ReadByte();
            Unknown17 = br.ReadByte();
            Unknown18 = br.ReadByte();
            Unknown19 = br.ReadByte();
            Unknown20 = br.ReadByte();
            Unknown21 = br.ReadByte();
            Unknown22 = br.ReadByte();
            Unknown23 = br.ReadByte();
            Stats = br.ReadSingleArray(ItemsCount).Select(c => new Stat(br.ReadUInt16(), c)).ToArray();
            Unknown40 = br.ReadUInt16();
            Unknown41 = br.ReadByte();
            Unknown42 = br.ReadUInt32();
        }

        private const byte ItemsCount = 8;
    }
}