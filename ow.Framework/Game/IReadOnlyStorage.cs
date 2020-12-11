using System.Collections.Generic;

namespace ow.Framework.Game
{
    internal interface IReadOnlyStorage : IReadOnlyList<IReadOnlyItem>
    {
    }

    internal interface IReadOnlyEquipableViewFashionStorage : IReadOnlyStorage { }

    internal interface IReadOnlyEquipableBattleFashionStorage : IReadOnlyStorage { }

    internal interface IReadOnlyEquipableGearStorage : IReadOnlyStorage { }
}