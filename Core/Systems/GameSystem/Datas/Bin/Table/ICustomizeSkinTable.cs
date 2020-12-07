using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using SoulWorker.Types;
using System.Collections.Generic;

namespace Core.Systems.GameSystem.Datas.Bin.Table
{
    using KeyType = HeroType;

    public interface ICustomizeSkinTable : IReadOnlyList<CustomizeSkinTableEntity>
    {
        public CustomizeSkinTableEntity this[KeyType index] => this[(int)index];
    }
}