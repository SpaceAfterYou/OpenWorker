using SoulWorker.Types;
using System.Collections.Generic;

namespace GateService.Systems.GameSystem
{
    public interface IGearEquippedStorage : IReadOnlyList<Item>
    {
        public Item Head => this[(int)EquippedGearSlotType.Head];
        public Item Shoulderguard => this[(int)EquippedGearSlotType.Shoulderguard];
        public Item Chest => this[(int)EquippedGearSlotType.Chest];
        public Item Leg => this[(int)EquippedGearSlotType.Leg];
        public Item Earrings => this[(int)EquippedGearSlotType.Earrings];
        public Item Talisma => this[(int)EquippedGearSlotType.Talisman];
        public Item Ring1 => this[(int)EquippedGearSlotType.Ring1];
        public Item Ring2 => this[(int)EquippedGearSlotType.Ring2];
        public Item Weapon => this[(int)EquippedGearSlotType.Weapon];
    }
}