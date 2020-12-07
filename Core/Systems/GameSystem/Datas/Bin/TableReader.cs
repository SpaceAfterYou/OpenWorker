using Core.Systems.GameSystem.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core.Systems.GameSystem.Datas.Bin
{
    internal static class TableReader<TId, TItem, TReturn>
        where TId : IConvertible
        where TItem : ITableItemEntry<TId>
        where TReturn : IReadOnlyList<TItem>
    {
        internal static TReturn Read(VData12 data, string file)
        {
            TItem[] items = new TItem[(dynamic)GetStaticFieldValue("MaxValue")];

            PropertyInfo[] properties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (BinaryReader br in GetItems(data, file))
            {
                TItem item = (TItem)Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br });
                items[(dynamic)item.Id] = item;
            }

            TId maxId = items.Select(p => p.Id).Max();
            return (dynamic)items[..((dynamic)maxId + 1)];
        }

        private static TId GetStaticFieldValue(string name) =>
            (TId)typeof(TId).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);

        private static IEnumerable<BinaryReader> GetItems(VData12 data, string file)
        {
            using BinaryReader br = new(data.GetInputStream(data.GetEntry($"../bin/Table/{file}.res")));

            uint count = br.ReadUInt32();
            for (uint q = 0; q < count; ++q)
            {
                yield return br;
            }
        }
    }
}