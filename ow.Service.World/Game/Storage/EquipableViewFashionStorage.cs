using ow.Framework.Database.Storages;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System;
using System.Collections.Generic;

namespace SoulCore.Game.Storage
{
    public sealed class EquipableViewFashionStorage : BaseStorage
    {
        internal static readonly EquipableViewFashionStorage Empty = new();

        public EquipableViewFashionStorage(IEnumerable<ItemModel> values, BinTable tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }

        private EquipableViewFashionStorage() : base(Array.Empty<ItemModel>(), BinTable.Empty, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}