using System;

namespace SoulWorker
{
    public interface ITableEntity<Id> where TId : IConvertible
    {
        TId Id { get; }
    }
}
