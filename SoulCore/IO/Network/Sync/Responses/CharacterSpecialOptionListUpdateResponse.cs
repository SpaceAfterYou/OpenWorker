﻿using SoulCore.Game.Enums;
using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record CharacterSpecialOptionListUpdateResponse
    {
        public readonly struct Entity
        {
            public SpecialOption Id { get; init; }
            public float Value { get; init; }
        }

        public int Character { get; init; }
        public IEnumerable<Entity> Values { get; init; } = default!;
    }
}
