using ow.Framework.Database.Storages;
using System.Collections.Generic;

namespace ow.Framework.FS.Storage
{
    public sealed class EquipableBattleFashionStorage : BaseStorage
    {
        public EquipableBattleFashionStorage(IEnumerable<ItemModel> values) : base(values, MaxCapacity)
        {
        }

        public const ushort MaxCapacity = 14;
    }
}