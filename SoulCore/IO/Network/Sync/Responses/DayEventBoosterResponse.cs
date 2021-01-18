using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record DayEventBoosterResponse
    {
        public sealed record Entity
        {
            public ushort Id { get; init; }
            public ushort Maze { get; init; }
        }

        public IReadOnlyList<Entity> Values { get; init; } = default!;
    }
}
