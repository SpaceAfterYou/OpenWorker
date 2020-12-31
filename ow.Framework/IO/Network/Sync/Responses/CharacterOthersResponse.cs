using ow.Framework.IO.Network.Sync.Responses.Shared;
using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record CharacterOthersResponse
    {
        public readonly struct Entity
        {
            public CharacterShared Character { get; init; }
            public PlaceShared Place { get; init; }
        }

        public IEnumerable<Entity> Values { get; init; } = default!;
    }
}