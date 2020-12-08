using System;

namespace Core.Systems.GameSystem.Datas.Bin
{
    public interface ITableEntity<TId> where TId : IConvertible
    {
        TId Id { get; }
    }
}