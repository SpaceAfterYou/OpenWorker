using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record CharacterStatsUpdateResponse
    {
        public sealed record Entity
        {
            public CharacterStat Id { get; init; }
            public float Value { get; init; }
        }

        public int Character { get; init; }
        public IReadOnlyCollection<Entity> Values { get; init; } = default!;
    }
}