namespace ow.Framework.Game.Storage
{
    public interface IStorage
    {
        public IViewFashionStorage EquippedViewFashionStorage { get; }

        public IEquipableBattleFashionStorage EquippedBattleFashionStorage { get; }

        public IEquipableGearStorage EquippedGearStorage { get; }
    }
}