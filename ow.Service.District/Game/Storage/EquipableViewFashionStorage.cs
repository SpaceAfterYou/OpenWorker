using SoulCore.Database.Storages;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System.Collections.Generic;

namespace SoulCore.Game.Storage
{
    public sealed class EquipableViewFashionStorage : BaseStorage
    {
        public EquipableViewFashionStorage(IEnumerable<ItemModel> values, BinTables tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}
