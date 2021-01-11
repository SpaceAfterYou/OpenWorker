using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public class EquipableGearStorage : BaseStorage
    {
        public StorageItem this[EquippedGearSlot index] => this[(int)index]!;

        public StorageItem Head => this[EquippedGearSlot.Head];
        public StorageItem Shoulderguard => this[EquippedGearSlot.Shoulderguard];
        public StorageItem Chest => this[EquippedGearSlot.Chest];
        public StorageItem Leg => this[EquippedGearSlot.Leg];
        public StorageItem Earrings => this[EquippedGearSlot.Earrings];
        public StorageItem Talisman => this[EquippedGearSlot.Talisman];
        public StorageItem Ring1 => this[EquippedGearSlot.Ring1];
        public StorageItem Ring2 => this[EquippedGearSlot.Ring2];
        public StorageItem Weapon => this[EquippedGearSlot.Weapon];

        public EquipableGearStorage(IEnumerable<ItemModel> values, BinTables tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}