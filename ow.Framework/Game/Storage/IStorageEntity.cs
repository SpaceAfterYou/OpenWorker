using System;

namespace ow.Framework.Game.Storage
{
    public interface IStorageEntity
    {
        EquipableBattleFashionStorage EquippedBattleFashionStorage { get => throw new NotImplementedException(); }
        EquipableViewFashionStorage EquippedViewFashionStorage { get => throw new NotImplementedException(); }
        EquipableGearStorage EquippedGearStorage { get => throw new NotImplementedException(); }
    }
}