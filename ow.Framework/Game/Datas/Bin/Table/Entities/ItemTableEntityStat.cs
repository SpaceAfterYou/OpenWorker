namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    internal readonly struct ItemTableEntityStat : IItemTableEntityStat
    {
        public uint Id { get; }
        public int Value { get; }

        internal ItemTableEntityStat(uint id, int value) => (Id, Value) = (id, value);
    }
}