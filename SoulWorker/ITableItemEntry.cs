using System;

namespace SoulWorker
{
    public interface ITableItemEntry<TId> where TId : IConvertible
    {
        TId Id { get; }
    }
}