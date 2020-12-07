using System;

namespace Core.Systems.GameSystem.Datas.Bin
{
    public interface ITableItemEntry<TId> where TId : IConvertible
    {
        TId Id { get; }
    }
}