using Core.Systems.GameSystem.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core.Systems.GameSystem.Datas.Bin
{
    internal static class TableReader<TId, TItem>
        where TId : IConvertible
        where TItem : ITableItemEntry<TId>
    {
        internal static IReadOnlyDictionary<TId, TItem> Read(VData12 data, string file)
        {
            List<TItem> items = new(ushort.MaxValue);

            foreach (BinaryReader br in GetItems(data, file))
            {
                TItem item = (TItem)Activator.CreateInstance(typeof(TItem), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br }, null);
                items.Add(item);
            }

            return items.ToDictionary(k => k.Id, v => v);
        }

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