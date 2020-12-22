using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    internal class EquipableGearStorage : BaseStorage
    {
        internal StorageItem this[EquippedGearSlot index] => this[(int)index]!;

        internal StorageItem Head => this[EquippedGearSlot.Head];
        internal StorageItem Shoulderguard => this[EquippedGearSlot.Shoulderguard];
        internal StorageItem Chest => this[EquippedGearSlot.Chest];
        internal StorageItem Leg => this[EquippedGearSlot.Leg];
        internal StorageItem Earrings => this[EquippedGearSlot.Earrings];
        internal StorageItem Talisman => this[EquippedGearSlot.Talisman];
        internal StorageItem Ring1 => this[EquippedGearSlot.Ring1];
        internal StorageItem Ring2 => this[EquippedGearSlot.Ring2];
        internal StorageItem Weapon => this[EquippedGearSlot.Weapon];

        internal EquipableGearStorage(IEnumerable<ItemModel> values, BinTables tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}