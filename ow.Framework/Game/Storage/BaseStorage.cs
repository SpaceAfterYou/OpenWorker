using System.Collections.Generic;

namespace ow.Framework.Game.Storage
{
    public abstract class BaseStorage : List<ItemStorage>
    {
        public byte Slots { get; }
        public byte Upgrades { get; }

        public BaseStorage(IEnumerable<ItemStorage> items) : base(items)
        { }
    }
}