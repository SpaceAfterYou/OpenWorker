using ow.Framework.Game.Enums;
using System;
using System.Linq;

namespace ow.Framework.Game.Storage
{
    public class EquipableGearStorage : BaseStorage
    {
        public ItemStorage this[EquippedGearSlot index] => this[(int)index];

        public ItemStorage Head => this[(int)EquippedGearSlot.Head];
        public ItemStorage Shoulderguard => this[(int)EquippedGearSlot.Shoulderguard];
        public ItemStorage Chest => this[(int)EquippedGearSlot.Chest];
        public ItemStorage Leg => this[(int)EquippedGearSlot.Leg];
        public ItemStorage Earrings => this[(int)EquippedGearSlot.Earrings];
        public ItemStorage Talisma => this[(int)EquippedGearSlot.Talisman];
        public ItemStorage Ring1 => this[(int)EquippedGearSlot.Ring1];
        public ItemStorage Ring2 => this[(int)EquippedGearSlot.Ring2];
        public ItemStorage Weapon => this[(int)EquippedGearSlot.Weapon];

        public EquipableGearStorage() : base(Enumerable.Repeat<ItemStorage>(null, (int)Enum.GetValues(typeof(EquippedGearSlot)).Cast<EquippedGearSlot>().Max()))
        {
        }
    }
}