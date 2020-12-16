using System;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    public readonly struct BoosterTableEntityStat
    {
        public ushort Id { get; }
        public float Value { get; }

        internal BoosterTableEntityStat(ushort id, float value) => (Id, Value) = (id, value);
    }
}