using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace Core.Systems.GameSystem.Datas.Bin
{
    internal static class TableReader<TId, TItem>
        where TId : IConvertible
        where TItem : ITableItemEntry<TId>
    {
        internal static IReadOnlyList<TItem> Read(VArchive archive, string file)
        {
            TItem[] items = new TItem[(dynamic)GetStaticFieldValue("MaxValue")];

            PropertyInfo[] properties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (BinaryReader br in GetItems(archive, file))
            {
                TItem item = (TItem)Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br });
                items[(dynamic)item.Id] = item;
            }

            TId maxId = items.Select(p => p.Id).Max();
            return items[..((dynamic)maxId + 1)];
        }

        private static TId GetStaticFieldValue(string name) =>
            (TId)typeof(TId).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);

        private static IEnumerable<BinaryReader> GetItems(VArchive archive, string file)
        {
            using BinaryReader br = new(archive.GetInputStream(archive.GetEntry($"../bin/Table/{file}.res")));

            uint count = br.ReadUInt32();
            for (uint q = 0; q < count; ++q)
            {
                yield return br;
            }
        }
    }
}