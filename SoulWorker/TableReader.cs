using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace SoulWorker
{
    internal static class TableReader<TId, TItem>
        where TId : IConvertible
        where TItem : ITableItemEntry<TId>
    {
        internal static IReadOnlyList<TItem> Read(VArchive archive, string file)
        {
            TId maxId = GetStaticFieldValue("MinValue");
            TItem[] items = new TItem[(dynamic)GetStaticFieldValue("MaxValue")];

            foreach (BinaryReader br in GetItems(archive, file))
            {
                TItem item = (TItem)Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br });
                maxId = Math.Max((dynamic)item.Id, (dynamic)maxId);
                items[(dynamic)item.Id] = item;
            }

            return items[0..((dynamic)maxId + 1)];
        }

        private static TId GetStaticFieldValue(string name) =>
            (TId)typeof(TId).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);

        private static IEnumerable<BinaryReader> GetItems(VArchive archive, string file)
        {
            ZipEntry res = archive[$"../bin/Table/{file}.res"];

            using MemoryStream ms = new((int)res.UncompressedSize);
            res.Extract(ms);

            ms.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new(ms);

            uint count = br.ReadUInt32();
            for (uint q = 0; q < count; ++q)
            {
                yield return br;
            }
        }
    }
}