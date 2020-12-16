using ow.Framework.Game.Datas.Bin.Table.Entities;
using System;

namespace ow.Service.District.Game
{
    public record Booster
    {
        public BoosterTableEntity Value { get; }
        public DateTimeOffset End { get; }

        internal Booster(BoosterTableEntity value, DateTimeOffset end) => (Value, End) = (value, end);
    }
}