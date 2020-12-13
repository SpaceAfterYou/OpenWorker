using System;

namespace ow.Framework.Game.Storage
{
    public interface IStorage
    {
        public EquipableBattleFashionStorage EquippedBattleFashionStorage { get => throw new NotImplementedException(); }
        public EquipableViewFashionStorage EquippedViewFashionStorage { get => throw new NotImplementedException(); }
        public EquipableGearStorage EquippedGearStorage { get => throw new NotImplementedException(); }
    }
}