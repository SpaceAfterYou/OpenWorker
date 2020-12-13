using ow.Framework.Game.Enums;

namespace ow.Framework.Game
{
    public interface IReadOnlyEquipableGearStorage : IReadOnlyItems
    {
        IItem this[EquippedGearSlot index] => this[(int)index];

        IItem Head => this[(int)EquippedGearSlot.Head];
        IItem Shoulderguard => this[(int)EquippedGearSlot.Shoulderguard];
        IItem Chest => this[(int)EquippedGearSlot.Chest];
        IItem Leg => this[(int)EquippedGearSlot.Leg];
        IItem Earrings => this[(int)EquippedGearSlot.Earrings];
        IItem Talisma => this[(int)EquippedGearSlot.Talisman];
        IItem Ring1 => this[(int)EquippedGearSlot.Ring1];
        IItem Ring2 => this[(int)EquippedGearSlot.Ring2];
        IItem Weapon => this[(int)EquippedGearSlot.Weapon];
    }
}