using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public class EquipableGearStorage : BaseStorage
    {
        public ItemStorage this[EquippedGearSlot index] => this[(int)index];

        public ItemStorage Head => this[EquippedGearSlot.Head];
        public ItemStorage Shoulderguard => this[EquippedGearSlot.Shoulderguard];
        public ItemStorage Chest => this[EquippedGearSlot.Chest];
        public ItemStorage Leg => this[EquippedGearSlot.Leg];
        public ItemStorage Earrings => this[EquippedGearSlot.Earrings];
        public ItemStorage Talisma => this[EquippedGearSlot.Talisman];
        public ItemStorage Ring1 => this[EquippedGearSlot.Ring1];
        public ItemStorage Ring2 => this[EquippedGearSlot.Ring2];
        public ItemStorage Weapon => this[EquippedGearSlot.Weapon];

        public EquipableGearStorage(IEnumerable<ItemModel> values) : base(values, MaxCapacity)
        {
        }

        public const ushort MaxCapacity = 10;
    }
}