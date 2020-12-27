using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record GestureLoadResponse
    {
        public IReadOnlyList<uint> Values { get; init; } = default!;
    }
}
