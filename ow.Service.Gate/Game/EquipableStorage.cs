using ow.Framework.Game;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal class EquipableStorage : List<IReadOnlyItem>, IReadOnlyEquipableViewFashionStorage, IReadOnlyEquipableBattleFashionStorage, IReadOnlyEquipableGearStorage
    {
        internal EquipableStorage(IReadOnlyList<Item> items) : base(items)
        {
        }
    }
}