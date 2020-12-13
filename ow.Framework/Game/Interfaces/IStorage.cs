namespace ow.Framework.Game
{
    public interface IStorage
    {
        public IReadOnlyEquipableViewFashionStorage EquippedViewFashionStorage { get; }

        public IReadOnlyEquipableBattleFashionStorage EquippedBattleFashionStorage { get; }

        public IReadOnlyEquipableGearStorage EquippedGearStorage { get; }
    }
}