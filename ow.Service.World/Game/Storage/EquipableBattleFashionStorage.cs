using ow.Framework.Database.Storages;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System;
using System.Collections.Generic;

namespace SoulCore.Game.Storage
{
    public sealed class EquipableBattleFashionStorage : BaseStorage
    {
        internal static readonly EquipableBattleFashionStorage Empty = new();

        public EquipableBattleFashionStorage(IEnumerable<ItemModel> values, BinTable tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }

        private EquipableBattleFashionStorage() : base(Array.Empty<ItemModel>(), BinTable.Empty, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}