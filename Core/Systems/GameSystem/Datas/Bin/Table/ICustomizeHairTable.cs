using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using SoulWorker.Types;
using System.Collections.Generic;

namespace Core.Systems.GameSystem.Datas.Bin.Table
{
    using KeyType = HeroType;

    public interface ICustomizeHairTable : IReadOnlyList<CustomizeHairTableEntity>
    {
        public CustomizeHairTableEntity this[KeyType index] => this[(int)index];
    }
}