namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    public readonly struct ItemTableEntityStat
    {
        public uint Id { get; }
        public int Value { get; }

        internal ItemTableEntityStat(uint id, int value) => (Id, Value) = (id, value);
    }
}