using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public sealed class EquipableBattleFashionStorage : BaseStorage
    {
        public EquipableBattleFashionStorage(IReadOnlyList<ItemStorage> values) : base(values)
        {
        }
    }
}