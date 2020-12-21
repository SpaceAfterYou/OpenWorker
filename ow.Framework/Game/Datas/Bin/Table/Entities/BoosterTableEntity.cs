using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    public static class MyFunkyExtensions
    {
        public static IEnumerable<TResult> Zip<T1, T2, T3, T4, TResult>(
            this IEnumerable<T1> source,
            IEnumerable<T2> second,
            IEnumerable<T3> third,
            IEnumerable<T4> fourth,
            Func<T1, T2, T3, T4, TResult> func)
        {
            IEnumerator<T1> e1 = source.GetEnumerator();
            IEnumerator<T2> e2 = second.GetEnumerator();
            IEnumerator<T3> e3 = third.GetEnumerator();
            IEnumerator<T4> e4 = fourth.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext() && e4.MoveNext())
                yield return func(e1.Current, e2.Current, e3.Current, e4.Current);
        }
    }

    public sealed record BoosterTableEntity : ITableEntity<KeyType>
    {
        public readonly struct Effect
        {
            public byte EffectType { get; }
            public byte ApplyType { get; }
            public float EffectValue { get; }
            public ushort EffectString { get; }

            internal Effect(byte effectType, byte applyType, float effectValue, ushort effectString) => (EffectType, ApplyType, EffectValue, EffectString) = (effectType, applyType, effectValue, effectString);
        }

        public KeyType Id { get; }
        public ushort Group { get; }
        public IReadOnlyList<Effect> Effects { get; }
        public ushort Info { get; }
        public byte DecreaseCondition { get; }
        public uint Time { get; }

        internal BoosterTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Group = br.ReadUInt16();

            IEnumerable<byte> effectTypes = br.ReadByteArray(ItemsCount);
            IEnumerable<byte> applyTypes = br.ReadByteArray(ItemsCount);
            IEnumerable<float> effectValues = br.ReadSingleArray(ItemsCount);
            IEnumerable<ushort> effectStrings = br.ReadUInt16Array(ItemsCount);

            Effects = GetItems(effectTypes, applyTypes, effectValues, effectStrings).Select(x => new Effect(x.Item1, x.Item2, x.Item3, x.Item4)).ToArray();

            Info = br.ReadUInt16();
            DecreaseCondition = br.ReadByte();
            Time = br.ReadUInt32();
        }

        private static IEnumerable<Tuple<T1, T2, T3, T4>> GetItems<T1, T2, T3, T4>(IEnumerable<T1> first, IEnumerable<T2> second, IEnumerable<T3> third, IEnumerable<T4> fourth)
        {
            IEnumerator<T1> e1 = first.GetEnumerator();
            IEnumerator<T2> e2 = second.GetEnumerator();
            IEnumerator<T3> e3 = third.GetEnumerator();
            IEnumerator<T4> e4 = fourth.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext() && e4.MoveNext())
                yield return Tuple.Create(e1.Current, e2.Current, e3.Current, e4.Current);
        }

        private const byte ItemsCount = 8;
    }
}