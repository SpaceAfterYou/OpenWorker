using ow.Framework.Game.Enums;

namespace ow.Framework.Game
{
    public interface IReadOnlyEquipableViewFashionStorage : IReadOnlyItems
    {
        public IReadOnlyItem Head => this[(int)EquippedGearSlot.Head];
        public IReadOnlyItem Shoulderguard => this[(int)EquippedGearSlot.Shoulderguard];
        public IReadOnlyItem Chest => this[(int)EquippedGearSlot.Chest];
        public IReadOnlyItem Leg => this[(int)EquippedGearSlot.Leg];
        public IReadOnlyItem Earrings => this[(int)EquippedGearSlot.Earrings];
        public IReadOnlyItem Talisma => this[(int)EquippedGearSlot.Talisman];
        public IReadOnlyItem Ring1 => this[(int)EquippedGearSlot.Ring1];
        public IReadOnlyItem Ring2 => this[(int)EquippedGearSlot.Ring2];
        public IReadOnlyItem Weapon => this[(int)EquippedGearSlot.Weapon];
    }
}