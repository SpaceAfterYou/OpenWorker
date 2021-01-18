using System;

namespace SoulCore.Game.Datas.Bin
{
    public interface ITableEntity<TId> where TId : IConvertible
    {
        TId Id { get; }
    }
}
