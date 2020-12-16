using System;

namespace ow.Service.District.Game
{
    public record Booster
    {
        public BoosterTableEntity Booster { get; }
        public DateTimeOffset End { get; }
    }
}