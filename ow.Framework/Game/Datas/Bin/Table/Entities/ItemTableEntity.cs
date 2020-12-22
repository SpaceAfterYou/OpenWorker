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
        public readonly struct Option
        {
            public byte Class { get; }
            public uint Type { get; }
            public int Value { get; }

            internal Option(byte @class, uint id, int value) => (Class, Type, Value) = (@class, id, value);
        }

        public readonly struct Specification
        {
            public uint Min { get; }
            public uint Max { get; }
            public uint Magic { get; }

            internal Specification(uint min, uint max, uint magic) => (Min, Max, Magic) = (min, max, magic);
        }

        public static ItemTableEntity Empty => _empty;
        private static readonly ItemTableEntity _empty = new();

        public KeyType Id { get; }
        public uint Classify { get; }
        public byte Unknown7 { get; }
        public byte MaxSlots { get; }
        public ushort Unknown9 { get; }
        public uint SellPrice { get; }
        public uint Unknown11 { get; }
        public uint Unknown12 { get; }
        public uint Unknown13 { get; }
        public ushort StackMax { get; }
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
        public string SlotDisable { get; }
        public byte Endurance { get; }
        public byte UseValue { get; }
        public Specification AttackDamage { get; }
        public Specification Defence { get; }
        public IReadOnlyList<Option> Options { get; }
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
            StackMax = br.ReadUInt16();
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
            SlotDisable = br.ReadByteLengthUnicodeString();
            Endurance = br.ReadByte();
            UseValue = br.ReadByte();
            AttackDamage = new(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());
            Defence = new(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());
            Options = ReadOptions(br);
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

        private ItemTableEntity()
        {
            SlotDisable = "0";
            Options = Enumerable.Repeat(new Option(), Defines.StatsPerItem).ToArray();
        }

        private static Option[] ReadOptions(BinaryReader br)
        {
            IEnumerable<byte> classes = br.ReadByteArray(Defines.StatsPerItem);
            IEnumerable<uint> types = br.ReadUInt32Array(Defines.StatsPerItem);
            IEnumerable<int> values = br.ReadInt32Array(Defines.StatsPerItem);

            return GetItems(classes, types, values).Select(item => new Option(item.Item1, item.Item2, item.Item3)).ToArray();
        }

        private static IEnumerable<Tuple<T1, T2, T3>> GetItems<T1, T2, T3>(IEnumerable<T1> first, IEnumerable<T2> second, IEnumerable<T3> third)
        {
            IEnumerator<T1> e1 = first.GetEnumerator();
            IEnumerator<T2> e2 = second.GetEnumerator();
            IEnumerator<T3> e3 = third.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
                yield return Tuple.Create(e1.Current, e2.Current, e3.Current);
        }
    }
}