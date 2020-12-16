using ow.Framework.Game.Datas.Bin.Table.Entities;
using System;

namespace ow.Service.District.Game
{
    public record Booster
    {
        public BoosterTableEntity Prototype { get; }
        public DateTimeOffset End { get; }

        internal Booster(BoosterTableEntity prototype, DateTimeOffset end) => (Prototype, End) = (prototype, end);
    }
}