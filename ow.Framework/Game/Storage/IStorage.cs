using System;

namespace ow.Framework.Game.Storage
{
    public interface IStorage
    {
        public ViewFashionStorage EquippedViewFashionStorage { get => throw new NotImplementedException(); }

        public EquipableBattleFashionStorage EquippedBattleFashionStorage { get => throw new NotImplementedException(); }

        public EquipableGearStorage EquippedGearStorage { get => throw new NotImplementedException(); }
    }
}