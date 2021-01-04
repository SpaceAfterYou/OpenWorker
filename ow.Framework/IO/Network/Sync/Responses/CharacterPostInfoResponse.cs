using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record CharacterPostInfoResponse
    {
        public IEnumerable<object> Values { get; init; } = default!;
    }
}