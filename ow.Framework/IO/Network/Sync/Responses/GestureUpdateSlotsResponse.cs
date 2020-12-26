using System.Collections.Generic;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record GestureUpdateSlotsResponse
    {
        public IEnumerable<uint> Values { get; init; } = default!;
    }
}