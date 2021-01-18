using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record CharacterGestureUpdateSlotsResponse
    {
        public IEnumerable<uint> Values { get; init; } = default!;
    }
}
