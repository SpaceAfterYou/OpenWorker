using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record CharacterGestureLoad
    {
        public IEnumerable<uint> Values { get; init; } = default!;
    }
}