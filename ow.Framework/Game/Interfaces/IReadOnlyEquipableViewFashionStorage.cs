using ow.Framework.Game.Enums;

namespace ow.Framework.Game
{
    public interface IReadOnlyEquipableViewFashionStorage : IReadOnlyItems
    {
        public IItem Head => this[(int)EquippedGearSlot.Head];
        public IItem Shoulderguard => this[(int)EquippedGearSlot.Shoulderguard];
        public IItem Chest => this[(int)EquippedGearSlot.Chest];
        public IItem Leg => this[(int)EquippedGearSlot.Leg];
        public IItem Earrings => this[(int)EquippedGearSlot.Earrings];
        public IItem Talisma => this[(int)EquippedGearSlot.Talisman];
        public IItem Ring1 => this[(int)EquippedGearSlot.Ring1];
        public IItem Ring2 => this[(int)EquippedGearSlot.Ring2];
        public IItem Weapon => this[(int)EquippedGearSlot.Weapon];
    }
}