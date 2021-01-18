using SoulCore.Database.Storages;
using SoulCore.Game.Enums;
using ow.Service.District.Game;
using ow.Service.District.Game.Storage;
using System.Collections.Generic;

namespace SoulCore.Game.Storage
{
    public class EquipableGearStorage : BaseStorage
    {
        public StorageItem this[StorageEquippedGearSlot index] => this[(int)index]!;

        public StorageItem Head => this[StorageEquippedGearSlot.Head];
        public StorageItem Shoulderguard => this[StorageEquippedGearSlot.Shoulderguard];
        public StorageItem Chest => this[StorageEquippedGearSlot.Chest];
        public StorageItem Leg => this[StorageEquippedGearSlot.Leg];
        public StorageItem Earrings => this[StorageEquippedGearSlot.Earrings];
        public StorageItem Talisman => this[StorageEquippedGearSlot.Pendant];
        public StorageItem Ring1 => this[StorageEquippedGearSlot.Ring1];
        public StorageItem Ring2 => this[StorageEquippedGearSlot.Ring2];
        public StorageItem Weapon => this[StorageEquippedGearSlot.Weapon];

        public EquipableGearStorage(IEnumerable<ItemModel> values, BinTables tables) : base(values, tables, Defines.EquipableFashionStorageMaxCapacity)
        {
        }
    }
}
