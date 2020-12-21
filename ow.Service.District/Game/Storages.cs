using ow.Framework.Game.Storage;

namespace ow.Service.District.Game
{
    internal sealed record Storages
    {
        internal EquipableBattleFashionStorage EquippedBattleFashion { get; init; } = default!;
        internal EquipableViewFashionStorage EquippedViewFashion { get; init; } = default!;
        internal EquipableGearStorage EquippedGear { get; init; } = default!;
    }
}