using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using System;
using System.Collections.Generic;

namespace Core.Systems.GameSystem.Datas.Bin.Table
{
    using KeyType = UInt32;

    public interface IItemTable : IReadOnlyList<ItemTableEntity>
    {
        public ItemTableEntity this[KeyType index] => this[(int)index];
    }
}