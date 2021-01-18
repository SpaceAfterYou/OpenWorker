using SoulCore.Attributes;
using SoulCore.Extensions;
using SoulCore.Game.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_item")]
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
        public uint ClassifyId { get; }
        public byte Unknown7 { get; }
        public byte MaxSlots { get; }
        public ushort Unknown9 { get; }
        public uint SellPrice { get; }
        public uint BuyPrice { get; }
        public uint RecycleSellPrice { get; }
        public uint RecycleBuyPrice { get; }
        public ushort StackMax { get; }
        public byte BindType { get; }
        public uint Unknown16 { get; }
        public uint Unknown17 { get; }
        public uint Info { get; }
        public ushort MinLevel { get; }
        public Hero Hero { get; }
        public byte LimitSellType { get; }
        public byte SubType { get; }
        public byte CostumeSetType { get; }
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
        public ushort ItemReinforce { get; }
        public uint ReinforceOption { get; }
        public uint Title { get; }
        public uint Evolution { get; }
        public ushort Disassemble { get; }
        public uint Furniture { get; }
        public ushort CooltimeGroup { get; }
        public uint CooltimeValue { get; }
        public byte CooltimeSave { get; }
        public ushort EffectType { get; }
        public uint Effect { get; }
        public ushort Unknown63 { get; }
        public byte ItemCash { get; }
        public byte UsePeriodType { get; }
        public uint UsePeriodValue { get; }
        public byte Unknown67 { get; }
        public uint Unknown68 { get; }
        public byte SealingCnt { get; }
        public byte BreakCnt { get; }
        public uint SimilarGroup { get; }
        public uint Package { get; }

        internal ItemTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            ClassifyId = br.ReadUInt32();
            Unknown7 = br.ReadByte();
            MaxSlots = br.ReadByte();
            Unknown9 = br.ReadUInt16();
            SellPrice = br.ReadUInt32();
            BuyPrice = br.ReadUInt32();
            RecycleSellPrice = br.ReadUInt32();
            RecycleBuyPrice = br.ReadUInt32();
            StackMax = br.ReadUInt16();
            BindType = br.ReadByte();
            Unknown16 = br.ReadUInt32();
            Unknown17 = br.ReadUInt32();
            Info = br.ReadUInt32();
            MinLevel = br.ReadUInt16();
            Hero = br.ReadHero();
            LimitSellType = br.ReadByte();
            SubType = br.ReadByte();
            CostumeSetType = br.ReadByte();
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
            ItemReinforce = br.ReadUInt16();
            ReinforceOption = br.ReadUInt32();
            Title = br.ReadUInt32();
            Evolution = br.ReadUInt32();
            Disassemble = br.ReadUInt16();
            Furniture = br.ReadUInt32();
            CooltimeGroup = br.ReadUInt16();
            CooltimeValue = br.ReadUInt32();
            CooltimeSave = br.ReadByte();
            EffectType = br.ReadUInt16();
            Effect = br.ReadUInt32();
            Unknown63 = br.ReadUInt16();
            ItemCash = br.ReadByte();
            UsePeriodType = br.ReadByte();
            UsePeriodValue = br.ReadUInt32();
            Unknown67 = br.ReadByte();
            Unknown68 = br.ReadUInt32();
            SealingCnt = br.ReadByte();
            BreakCnt = br.ReadByte();
            SimilarGroup = br.ReadUInt32();
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
