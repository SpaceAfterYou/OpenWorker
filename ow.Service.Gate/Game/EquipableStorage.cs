using ow.Framework.Game.Storage;
using ow.Framework.Game.Storage.ItemStorage;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal class EquipableStorage : List<IItemStorage>, IViewFashionStorage, IEquipableBattleFashionStorage, IEquipableGearStorage
    {
        internal EquipableStorage(IReadOnlyList<Item> items) : base(items)
        {
        }
    }
}