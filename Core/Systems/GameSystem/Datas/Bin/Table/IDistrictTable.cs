using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using System;
using System.Collections.Generic;

namespace Core.Systems.GameSystem.Datas.Bin.Table
{
    using KeyType = UInt16;

    public interface IDistrictTable : IReadOnlyList<DistrictTableEntity>
    {
        public DistrictTableEntity this[KeyType index] => this[(int)index];
    }
}