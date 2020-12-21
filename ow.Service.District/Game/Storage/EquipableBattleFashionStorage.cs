using ow.Framework.Database.Storages;
using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public sealed class EquipableBattleFashionStorage : BaseStorage
    {
        public EquipableBattleFashionStorage(IEnumerable<ItemModel> values) : base(values, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}