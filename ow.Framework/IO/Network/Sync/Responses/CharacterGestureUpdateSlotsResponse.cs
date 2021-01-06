using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record CharacterGestureUpdateSlotsResponse
    {
        public IEnumerable<uint> Values { get; init; } = default!;
    }
}