using ow.Framework.Database.Storages;
using System.Collections.Generic;
using System.Linq;

namespace ow.Framework.Game.Storage
{
    public abstract class BaseStorage : List<ItemStorage>
    {
        public byte Slots { get; }
        public byte Upgrades { get; }

        public BaseStorage(IEnumerable<ItemModel> models, ushort maxCapacity) : base(Enumerable.Repeat((ItemStorage)null, maxCapacity))
        {
            foreach (ItemModel model in models)
                this[model.Slot] = new(model);
        }
    }
}