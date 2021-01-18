﻿using SoulCore.Game.Enums;
using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record ChannelInfoResponse
    {
        public readonly struct Entity
        {
            public ushort Id { get; init; }
            public ChannelLoadStatus Status { get; init; }
        }

        public ushort Location { get; init; }
        public IEnumerable<Entity> Values { get; init; } = default!;
    }
}
