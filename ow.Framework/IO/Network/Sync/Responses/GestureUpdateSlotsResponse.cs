using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record GestureUpdateSlotsResponse
    {
        public IEnumerable<uint> Values { get; init; } = default!;
    }
}
