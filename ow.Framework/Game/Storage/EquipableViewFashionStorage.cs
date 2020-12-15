using ow.Framework.Database.Storages;
using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public sealed class EquipableViewFashionStorage : BaseStorage
    {
        public EquipableViewFashionStorage(IEnumerable<ItemModel> values) : base(values, MaxCapacity)
        {
        }

        public const ushort MaxCapacity = 14;
    }
}