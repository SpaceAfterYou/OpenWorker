using System;

namespace SoulWorker
{
    public interface ITableItemEntry<Id> where TId : IConvertible
    {
        TId Id { get; }
    }
}