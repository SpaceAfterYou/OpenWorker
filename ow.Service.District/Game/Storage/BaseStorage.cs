using ow.Framework.Database.Storages;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Storage
{
    internal abstract class BaseStorage : List<StorageItem?>
    {
        internal new byte Capacity { get; set; }

        internal BaseStorage(IEnumerable<ItemModel> models, BinTables tables, ushort maxCapacity) : base(GetItems(models, tables, maxCapacity))
        {
        }

        private static IEnumerable<StorageItem?> GetItems(IEnumerable<ItemModel> models, BinTables tables, ushort maxCapacity)
        {
            StorageItem?[] items = Enumerable.Repeat((StorageItem?)null, maxCapacity).ToArray();

            foreach (ItemModel model in models)
                items[model.Slot] = new StorageItem(model, tables.Item[model.PrototypeId]);

            return items;
        }
    }
}