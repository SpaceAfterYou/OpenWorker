using SoulCore.Database.Storages;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System.Collections.Generic;

namespace SoulCore.Game.Storage
{
    public sealed class EquipableBattleFashionStorage : BaseStorage
    {
        public EquipableBattleFashionStorage(IEnumerable<ItemModel> values, BinTables tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}
