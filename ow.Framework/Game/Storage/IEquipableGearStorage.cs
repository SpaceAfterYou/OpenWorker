using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage.Item;

namespace ow.Framework.Game.Storage
{
    public interface IEquipableGearStorage : IItemsStorage
    {
        IItemStorage this[EquippedGearSlot index] => this[(int)index];

        IItemStorage Head => this[(int)EquippedGearSlot.Head];
        IItemStorage Shoulderguard => this[(int)EquippedGearSlot.Shoulderguard];
        IItemStorage Chest => this[(int)EquippedGearSlot.Chest];
        IItemStorage Leg => this[(int)EquippedGearSlot.Leg];
        IItemStorage Earrings => this[(int)EquippedGearSlot.Earrings];
        IItemStorage Talisma => this[(int)EquippedGearSlot.Talisman];
        IItemStorage Ring1 => this[(int)EquippedGearSlot.Ring1];
        IItemStorage Ring2 => this[(int)EquippedGearSlot.Ring2];
        IItemStorage Weapon => this[(int)EquippedGearSlot.Weapon];
    }
}