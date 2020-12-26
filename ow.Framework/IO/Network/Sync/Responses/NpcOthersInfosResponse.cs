using System.Collections.Generic;
using System.Numerics;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record NpcOthersInfosResponse
    {
        public sealed record Entity
        {
            public uint Id { get; init; }
            public Vector3 Position { get; init; }
            public float Rotation { get; init; }
            public uint Waypoint { get; init; }
            public uint NpcId { get; init; }
        }

        public IReadOnlyList<Entity> Values { get; init; } = default!;
    }
}